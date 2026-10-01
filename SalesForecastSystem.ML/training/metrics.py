"""Regression metrics used to evaluate demand forecasts."""

from __future__ import annotations

import math
from collections.abc import Sequence


def _validate(actual: Sequence[float], predicted: Sequence[float]) -> None:
    if len(actual) != len(predicted):
        raise ValueError("actual and predicted must contain the same number of values")
    if not actual:
        raise ValueError("at least one observation is required")
    if any(not math.isfinite(float(value)) for value in (*actual, *predicted)):
        raise ValueError("metric values must be finite numbers")


def mean_absolute_error(actual: Sequence[float], predicted: Sequence[float]) -> float:
    """Return mean(abs(actual - predicted))."""

    _validate(actual, predicted)
    return sum(abs(float(a) - float(p)) for a, p in zip(actual, predicted)) / len(actual)


def root_mean_squared_error(actual: Sequence[float], predicted: Sequence[float]) -> float:
    """Return sqrt(mean((actual - predicted) ** 2))."""

    _validate(actual, predicted)
    squared_error = sum((float(a) - float(p)) ** 2 for a, p in zip(actual, predicted))
    return math.sqrt(squared_error / len(actual))
