#!/usr/bin/env python3
"""
Automated visual UI correction loop for KILLERGAME.

1. Watches Unity source/scene changes (debounced).
2. Captures KillerGameScene via headless Unity batch capture -> current_state.png
3. Compares current_state.png vs image_1.png using Gemini Flash
4. Pipes structured feedback into Cursor (SDK or CLI) for the next edit pass
5. Repeats until Gemini returns STATUS: Match Confirmed

Environment variables:
  GEMINI_API_KEY          Required for vision comparison
  CURSOR_API_KEY          Required for cursor-sdk agent dispatch (recommended)
  UNITY_PATH              Optional Unity editor binary override
  CURSOR_CLI              Optional Cursor CLI path (fallback)
  UI_VISION_TARGET        Optional target reference image (default: ./image_1.png)
  UI_VISION_OUTPUT        Optional screenshot output path (also read by Unity capture)
  UI_VISION_MAX_ITER      Optional max loop iterations (default: 25)
  UI_VISION_DEBOUNCE_SEC  Optional debounce seconds (default: 4)
"""

from __future__ import annotations

import argparse
import json
import os
import re
import shlex
import subprocess
import sys
import threading
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parent
VISION_DIR = ROOT / ".ui_vision"
FEEDBACK_LOG = VISION_DIR / "feedback.log"
ITERATION_LOG = VISION_DIR / "iterations.jsonl"
PROMPT_OUT = VISION_DIR / "cursor_prompt.txt"

DEFAULT_UNITY = "/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity"
DEFAULT_CURSOR_CLI = "/Applications/Cursor.app/Contents/Resources/app/bin/cursor"
DEFAULT_GEMINI_MODEL = os.environ.get("GEMINI_MODEL", "gemini-2.0-flash")

WATCH_SUFFIXES = {".cs", ".unity", ".prefab", ".asset", ".uss", ".uxml"}
WATCH_DIRS = [
    ROOT / "assets" / "KILLERGAME",
    ROOT / "ProjectSettings",
]

GEMINI_PROMPT = """You are a mobile UI layout QA inspector for a Unity game (Whiteout Survival style).

Compare these two portrait screenshots:
- IMAGE A = CURRENT implementation (what we built)
- IMAGE B = TARGET reference layout (ground truth)

Return ONLY this structured block (no markdown fences):

STATUS: Match Confirmed | Needs Fixes
SUMMARY: <one sentence>
ERRORS:
- <element/region>: <exact spatial or visual fix with numbers when possible, e.g. "Top resource row offset Y -20px", "Bottom nav font size +4pt", "Tab bar background alpha should be 0.55">
- ...
PRIORITY: <highest impact fix first>

Rules:
- Use STATUS: Match Confirmed ONLY when layout, spacing, transparency, typography, and major UI regions match the target.
- Be specific with anchors, offsets, colors (RGBA), font sizes, and element names when inferable.
- Ignore minor compression artifacts and tiny snow particle differences.
- If a region is missing entirely, call it out explicitly.
"""


@dataclass
class GeminiVerdict:
    raw: str
    status: str
    errors: list[str]
    summary: str
    priority: str

    @property
    def matched(self) -> bool:
        return self.status.strip().lower() == "match confirmed"


def log(msg: str) -> None:
    ts = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    line = f"[{ts}] {msg}"
    print(line, flush=True)
    VISION_DIR.mkdir(parents=True, exist_ok=True)
    with FEEDBACK_LOG.open("a", encoding="utf-8") as f:
        f.write(line + "\n")


def resolve_target() -> Path:
    env = os.environ.get("UI_VISION_TARGET")
    if env:
        return Path(env).expanduser().resolve()
    return (ROOT / "image_1.png").resolve()


def resolve_output() -> Path:
    env = os.environ.get("UI_VISION_OUTPUT")
    if env:
        return Path(env).expanduser().resolve()
    return (ROOT / "current_state.png").resolve()


def resolve_unity() -> Path:
    env = os.environ.get("UNITY_PATH")
    path = Path(env).expanduser() if env else Path(DEFAULT_UNITY)
    if not path.exists():
        raise FileNotFoundError(
            f"Unity editor not found at {path}. Set UNITY_PATH to your Unity binary."
        )
    return path


def capture_screenshot(output: Path) -> None:
    unity = resolve_unity()
    VISION_DIR.mkdir(parents=True, exist_ok=True)
    unity_log = VISION_DIR / "unity_capture.log"

    env = os.environ.copy()
    env["UI_VISION_OUTPUT"] = str(output)

    cmd = [
        str(unity),
        "-batchmode",
        "-nographics",
        "-projectPath",
        str(ROOT),
        "-executeMethod",
        "KillerGame.Editor.UIVisionBatchCapture.Run",
        "-logFile",
        str(unity_log),
    ]

    log(f"Running Unity capture: {shlex.join(cmd)}")
    proc = subprocess.run(cmd, env=env, capture_output=True, text=True)
    if proc.returncode != 0:
        tail = unity_log.read_text(encoding="utf-8", errors="replace")[-4000:] if unity_log.exists() else proc.stderr
        raise RuntimeError(f"Unity capture failed (exit {proc.returncode}). Log tail:\n{tail}")

    if not output.exists():
        raise FileNotFoundError(f"Unity reported success but screenshot missing: {output}")


def _load_image_bytes(path: Path) -> bytes:
    if not path.exists():
        raise FileNotFoundError(f"Image not found: {path}")
    return path.read_bytes()


def compare_with_gemini(current: Path, target: Path, model_name: str) -> GeminiVerdict:
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        raise EnvironmentError("GEMINI_API_KEY is not set.")

    try:
        import google.generativeai as genai
    except ImportError as exc:
        raise ImportError(
            "Install dependencies: pip install -r requirements-ui-vision.txt"
        ) from exc

    genai.configure(api_key=api_key)
    model = genai.GenerativeModel(model_name)

    current_bytes = _load_image_bytes(current)
    target_bytes = _load_image_bytes(target)

    response = model.generate_content(
        [
            GEMINI_PROMPT,
            "IMAGE A (CURRENT):",
            {"mime_type": "image/png", "data": current_bytes},
            "IMAGE B (TARGET):",
            {"mime_type": "image/png", "data": target_bytes},
        ]
    )

    raw = (response.text or "").strip()
    return parse_gemini_verdict(raw)


def parse_gemini_verdict(raw: str) -> GeminiVerdict:
    status_match = re.search(r"^STATUS:\s*(.+)$", raw, re.MULTILINE | re.IGNORECASE)
    summary_match = re.search(r"^SUMMARY:\s*(.+)$", raw, re.MULTILINE | re.IGNORECASE)
    priority_match = re.search(r"^PRIORITY:\s*(.+)$", raw, re.MULTILINE | re.IGNORECASE)

    status = status_match.group(1).strip() if status_match else "Needs Fixes"
    summary = summary_match.group(1).strip() if summary_match else ""
    priority = priority_match.group(1).strip() if priority_match else ""

    errors: list[str] = []
    in_errors = False
    for line in raw.splitlines():
        if re.match(r"^ERRORS:\s*$", line, re.IGNORECASE):
            in_errors = True
            continue
        if in_errors:
            if re.match(r"^[A-Z_]+:\s*", line):
                break
            if line.strip().startswith("- "):
                errors.append(line.strip()[2:].strip())

    return GeminiVerdict(raw=raw, status=status, errors=errors, summary=summary, priority=priority)


def build_cursor_prompt(verdict: GeminiVerdict, iteration: int) -> str:
    errors_block = "\n".join(f"- {e}" for e in verdict.errors) or "- (see raw Gemini log)"
    return f"""UI vision loop iteration {iteration} — apply fixes in the Unity KILLERGAME project (KillerGameScene + WOSUIStyler/WOSVisualBootstrap/UIManager).

Gemini comparison vs image_1.png target:
STATUS: {verdict.status}
SUMMARY: {verdict.summary}
PRIORITY: {verdict.priority}

ERRORS:
{errors_block}

Instructions:
- Fix ONLY the listed UI/layout/camera issues.
- Keep transparent overlay architecture (no opaque full-screen blocks).
- After edits, ensure Play mode renders correctly at 1080x1920 portrait.
- Do not stop until STATUS would be Match Confirmed on the next vision pass.

Raw Gemini log:
{verdict.raw}
"""


def dispatch_to_cursor(prompt: str) -> None:
    VISION_DIR.mkdir(parents=True, exist_ok=True)
    PROMPT_OUT.write_text(prompt, encoding="utf-8")
    log(f"Wrote Cursor prompt to {PROMPT_OUT}")

    # Preferred: Cursor SDK local agent
    if os.environ.get("CURSOR_API_KEY"):
        try:
            from cursor_sdk import Agent, AgentOptions, LocalAgentOptions

            log("Dispatching feedback via cursor-sdk Agent.prompt")
            result = Agent.prompt(
                prompt,
                AgentOptions(
                    api_key=os.environ["CURSOR_API_KEY"],
                    model=os.environ.get("CURSOR_MODEL", "composer-2.5"),
                    local=LocalAgentOptions(cwd=str(ROOT)),
                ),
            )
            log(f"Cursor SDK finished with status={result.status}")
            if getattr(result, "result", None):
                log(f"Cursor SDK result snippet: {str(result.result)[:500]}")
            return
        except ImportError:
            log("cursor-sdk not installed; falling back to Cursor CLI if available")
        except Exception as exc:
            log(f"cursor-sdk dispatch failed: {exc}; trying CLI fallback")

    cli = Path(os.environ.get("CURSOR_CLI", DEFAULT_CURSOR_CLI))
    if cli.exists():
        # Pipe prompt on stdin where supported; also open prompt file for visibility.
        log(f"Dispatching feedback via Cursor CLI: {cli}")
        try:
            subprocess.run(
                [str(cli), str(PROMPT_OUT)],
                check=False,
                timeout=120,
            )
        except subprocess.TimeoutExpired:
            log("Cursor CLI call timed out (non-fatal). Prompt saved for manual/agent pickup.")
        return

    log("No Cursor dispatch backend available. Set CURSOR_API_KEY or CURSOR_CLI.")


def record_iteration(iteration: int, verdict: GeminiVerdict, current: Path, target: Path) -> None:
    VISION_DIR.mkdir(parents=True, exist_ok=True)
    payload = {
        "iteration": iteration,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "status": verdict.status,
        "matched": verdict.matched,
        "summary": verdict.summary,
        "priority": verdict.priority,
        "errors": verdict.errors,
        "current": str(current),
        "target": str(target),
    }
    with ITERATION_LOG.open("a", encoding="utf-8") as f:
        f.write(json.dumps(payload) + "\n")


def run_iteration(iteration: int, target: Path, output: Path, model: str, dispatch: bool) -> GeminiVerdict:
    log(f"=== Iteration {iteration} ===")
    capture_screenshot(output)
    log(f"Screenshot saved: {output}")

    verdict = compare_with_gemini(output, target, model)
    log(f"Gemini STATUS: {verdict.status}")
    log(verdict.raw)
    record_iteration(iteration, verdict, output, target)

    if verdict.matched:
        log("Match Confirmed — vision loop complete.")
        return verdict

    prompt = build_cursor_prompt(verdict, iteration)
    if dispatch:
        dispatch_to_cursor(prompt)
    else:
        PROMPT_OUT.write_text(prompt, encoding="utf-8")
        log(f"Dry-run: prompt written to {PROMPT_OUT}")

    return verdict


def run_loop(
    target: Path,
    output: Path,
    model: str,
    max_iter: int,
    dispatch: bool,
    once: bool,
) -> int:
    if not target.exists():
        log(f"ERROR: Target reference missing: {target}")
        log("Place your ground-truth UI mock as image_1.png in the repo root, or set UI_VISION_TARGET.")
        return 2

    iteration = 1
    while iteration <= max_iter:
        verdict = run_iteration(iteration, target, output, model, dispatch)
        if verdict.matched:
            return 0
        if once:
            return 1
        iteration += 1
        # Brief pause so Cursor/agent edits can land before the next capture.
        time.sleep(float(os.environ.get("UI_VISION_LOOP_PAUSE_SEC", "8")))

    log(f"Reached max iterations ({max_iter}) without Match Confirmed.")
    return 1


class DebouncedChangeHandler:
    def __init__(self, callback, debounce_sec: float) -> None:
        self.callback = callback
        self.debounce_sec = debounce_sec
        self._timer: threading.Timer | None = None
        self._lock = threading.Lock()

    def _should_handle(self, path: str) -> bool:
        p = Path(path)
        if p.suffix.lower() not in WATCH_SUFFIXES:
            return False
        if any(part.startswith(".") for part in p.parts):
            return False
        if ".ui_vision" in p.parts:
            return False
        resolved = str(p.resolve())
        return any(resolved.startswith(str(d.resolve())) for d in WATCH_DIRS)

    def on_change(self, path: str) -> None:
        if not self._should_handle(path):
            return
        with self._lock:
            if self._timer is not None:
                self._timer.cancel()
            self._timer = threading.Timer(self.debounce_sec, self.callback)
            self._timer.daemon = True
            self._timer.start()


def watch_and_loop(args: argparse.Namespace) -> int:
    target = resolve_target()
    output = resolve_output()
    max_iter = int(os.environ.get("UI_VISION_MAX_ITER", str(args.max_iter)))
    debounce = float(os.environ.get("UI_VISION_DEBOUNCE_SEC", str(args.debounce)))

    stop_flag = threading.Event()
    state = {"running": False}

    def trigger() -> None:
        if state["running"]:
            log("Change detected while iteration in progress; coalescing after current run.")
            return
        state["running"] = True
        try:
            code = run_loop(
                target=target,
                output=output,
                model=args.model,
                max_iter=max_iter,
                dispatch=not args.no_dispatch,
                once=args.once,
            )
            if code == 0:
                stop_flag.set()
        finally:
            state["running"] = False

    if args.watch:
        try:
            from watchdog.events import FileSystemEventHandler
            from watchdog.observers import Observer
        except ImportError as exc:
            raise ImportError("Install watchdog: pip install -r requirements-ui-vision.txt") from exc

        class Handler(FileSystemEventHandler):
            def __init__(self, debounced: DebouncedChangeHandler):
                self.debounced = debounced

            def on_modified(self, event):
                if not event.is_directory:
                    self.debounced.on_change(event.src_path)

            def on_created(self, event):
                if not event.is_directory:
                    self.debounced.on_change(event.src_path)

        debounced = DebouncedChangeHandler(trigger, debounce)
        handler = Handler(debounced)
        observer = Observer()
        for directory in WATCH_DIRS:
            if directory.exists():
                observer.schedule(handler, str(directory), recursive=True)
                log(f"Watching {directory}")
        observer.start()

        log("UI vision watch loop started. Edit files/scenes to trigger captures.")
        trigger()  # initial pass

        try:
            while not stop_flag.is_set():
                time.sleep(0.5)
        except KeyboardInterrupt:
            log("Interrupted by user.")
        finally:
            observer.stop()
            observer.join()
        return 0

    return run_loop(
        target=target,
        output=output,
        model=args.model,
        max_iter=max_iter,
        dispatch=not args.no_dispatch,
        once=args.once,
    )


def parse_args(argv: Iterable[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Automated UI vision correction loop")
    parser.add_argument("--watch", action="store_true", help="Watch repo changes and re-run continuously")
    parser.add_argument("--once", action="store_true", help="Run a single capture/compare/dispatch cycle")
    parser.add_argument("--no-dispatch", action="store_true", help="Skip Cursor dispatch; only write prompt file")
    parser.add_argument("--model", default=DEFAULT_GEMINI_MODEL, help="Gemini model id")
    parser.add_argument("--max-iter", type=int, default=25, help="Max correction iterations")
    parser.add_argument("--debounce", type=float, default=4.0, help="Debounce seconds for watch mode")
    return parser.parse_args(list(argv) if argv is not None else None)


def main() -> int:
    args = parse_args()
    try:
        return watch_and_loop(args)
    except KeyboardInterrupt:
        log("Stopped.")
        return 130
    except Exception as exc:
        log(f"FATAL: {exc}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
