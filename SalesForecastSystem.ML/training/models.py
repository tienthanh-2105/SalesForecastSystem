"""Small deterministic forecasting models for the MVP baseline."""

from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass
from datetime import date


@dataclass(frozen=True)
class Observation:
    date: date
    value: float


class ForecastModel(ABC):
    name: str
    version: str = "1.0.0"

    @property
    @abstractmethod
    def parameters(self) -> dict[str, int]:
        raise NotImplementedError

    @abstractmethod
    def predict_one(self, history: list[Observation], target_date: date) -> float:
        raise NotImplementedError


class MovingAverageModel(ForecastModel):
    name = "MovingAverage"

    def __init__(self, window: int) -> None:
        if window < 1:
            raise ValueError("moving-average window must be positive")
        self.window = window

    @property
    def parameters(self) -> dict[str, int]:
        return {"window": self.window}

    def predict_one(self, history: list[Observation], target_date: date) -> float:
        del target_date
        if not history:
            return 0.0
        values = [item.value for item in history[-self.window :]]
        return max(0.0, sum(values) / len(values))


class SeasonalNaiveModel(ForecastModel):
    name = "SeasonalNaive"

    def __init__(self, period: int) -> None:
        if period < 1:
            raise ValueError("seasonal period must be positive")
        self.period = period

    @property
    def parameters(self) -> dict[str, int]:
        return {"period": self.period}

    def predict_one(self, history: list[Observation], target_date: date) -> float:
        target = target_date.toordinal() - self.period
        by_ordinal = {item.date.toordinal(): item.value for item in history}
        if target in by_ordinal:
            return max(0.0, by_ordinal[target])
        return max(0.0, history[-1].value) if history else 0.0
