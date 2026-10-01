"""Command-line entry point for the Sales Forecast System ML baseline."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

from training.contracts import ForecastRequest
from training.pipeline import run_forecast


def execute(payload: dict[str, Any]) -> dict[str, Any]:
    return run_forecast(ForecastRequest.from_dict(payload))


def _arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Evaluate and run demand forecast baselines.")
    parser.add_argument("--input", type=Path, help="UTF-8 JSON request file; stdin when omitted")
    parser.add_argument("--output", type=Path, help="UTF-8 JSON result file; stdout when omitted")
    return parser.parse_args()


def main() -> int:
    arguments = _arguments()
    try:
        payload_text = (
            arguments.input.read_text(encoding="utf-8")
            if arguments.input
            else sys.stdin.read()
        )
        result = execute(json.loads(payload_text))
        rendered = json.dumps(result, ensure_ascii=False, indent=2)
        if arguments.output:
            arguments.output.parent.mkdir(parents=True, exist_ok=True)
            arguments.output.write_text(rendered + "\n", encoding="utf-8")
        else:
            print(rendered)
        return 0
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(json.dumps({"error": str(error)}, ensure_ascii=False), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
