"""Input and output contracts for the forecasting pipeline."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import date
from typing import Any


ELIGIBLE_STATUSES = {"Sold", "NoSale"}
VALID_SPLITS = {"Training", "Validation"}


@dataclass(frozen=True)
class DailyPoint:
    date: date
    quantity_sold: float
    data_status: str
    dataset_split: str

    @property
    def is_observed_demand(self) -> bool:
        return self.data_status in ELIGIBLE_STATUSES


@dataclass(frozen=True)
class ForecastRequest:
    product_id: int
    warehouse_id: int | None
    forecast_days: int
    points: tuple[DailyPoint, ...]
    moving_average_windows: tuple[int, ...] = (3, 7, 14)
    seasonal_periods: tuple[int, ...] = (7,)

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "ForecastRequest":
        try:
            points = tuple(
                DailyPoint(
                    date=date.fromisoformat(str(item["date"])),
                    quantity_sold=float(item["quantitySold"]),
                    data_status=str(item["dataStatus"]),
                    dataset_split=str(item["datasetSplit"]),
                )
                for item in payload["points"]
            )
            request = cls(
                product_id=int(payload["productId"]),
                warehouse_id=(
                    int(payload["warehouseId"])
                    if payload.get("warehouseId") is not None
                    else None
                ),
                forecast_days=int(payload.get("forecastDays", 7)),
                points=points,
                moving_average_windows=tuple(
                    int(value) for value in payload.get("movingAverageWindows", (3, 7, 14))
                ),
                seasonal_periods=tuple(
                    int(value) for value in payload.get("seasonalPeriods", (7,))
                ),
            )
        except (KeyError, TypeError, ValueError) as error:
            raise ValueError(f"invalid forecast request: {error}") from error
        request.validate()
        return request

    def validate(self) -> None:
        if self.product_id < 1:
            raise ValueError("productId must be positive")
        if self.warehouse_id is not None and self.warehouse_id < 1:
            raise ValueError("warehouseId must be positive")
        if not 1 <= self.forecast_days <= 366:
            raise ValueError("forecastDays must be between 1 and 366")
        if len(self.points) < 2:
            raise ValueError("at least two daily points are required")
        if not self.moving_average_windows and not self.seasonal_periods:
            raise ValueError("at least one candidate model is required")
        if any(value < 1 for value in (*self.moving_average_windows, *self.seasonal_periods)):
            raise ValueError("model parameters must be positive")

        previous_date: date | None = None
        training_seen = False
        validation_seen = False
        for point in self.points:
            if point.quantity_sold < 0:
                raise ValueError("quantitySold cannot be negative")
            if point.dataset_split not in VALID_SPLITS:
                raise ValueError(f"unsupported datasetSplit: {point.dataset_split}")
            if previous_date is not None:
                if point.date <= previous_date:
                    raise ValueError("points must be strictly ordered by date")
                if (point.date - previous_date).days != 1:
                    raise ValueError("points must form a continuous daily series")
            if point.dataset_split == "Training":
                if validation_seen:
                    raise ValueError("training points cannot appear after validation points")
                training_seen = True
            else:
                validation_seen = True
            previous_date = point.date

        if not training_seen or not validation_seen:
            raise ValueError("both Training and Validation points are required")
        if not any(
            point.dataset_split == "Training" and point.is_observed_demand
            for point in self.points
        ):
            raise ValueError("training split has no usable demand observations")
        if not any(
            point.dataset_split == "Validation" and point.is_observed_demand
            for point in self.points
        ):
            raise ValueError("validation split has no usable demand observations")
