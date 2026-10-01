"""Walk-forward evaluation, model selection, and future prediction."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import timedelta
from typing import Any

from training.contracts import ForecastRequest
from training.metrics import mean_absolute_error, root_mean_squared_error
from training.models import (
    ForecastModel,
    MovingAverageModel,
    Observation,
    SeasonalNaiveModel,
)


@dataclass(frozen=True)
class Evaluation:
    model: ForecastModel
    mae: float
    rmse: float
    evaluated_points: int


def _candidate_models(request: ForecastRequest) -> list[ForecastModel]:
    models: list[ForecastModel] = [
        MovingAverageModel(window) for window in request.moving_average_windows
    ]
    models.extend(SeasonalNaiveModel(period) for period in request.seasonal_periods)
    return models


def _evaluate(model: ForecastModel, request: ForecastRequest) -> Evaluation:
    history = [
        Observation(point.date, point.quantity_sold)
        for point in request.points
        if point.dataset_split == "Training" and point.is_observed_demand
    ]
    actual: list[float] = []
    predicted: list[float] = []
    for point in request.points:
        if point.dataset_split != "Validation":
            continue
        prediction = model.predict_one(history, point.date)
        if point.is_observed_demand:
            actual.append(point.quantity_sold)
            predicted.append(prediction)
            history.append(Observation(point.date, point.quantity_sold))

    return Evaluation(
        model=model,
        mae=mean_absolute_error(actual, predicted),
        rmse=root_mean_squared_error(actual, predicted),
        evaluated_points=len(actual),
    )


def run_forecast(request: ForecastRequest) -> dict[str, Any]:
    """Evaluate candidates, select the best, and forecast the requested horizon."""

    evaluations = [_evaluate(model, request) for model in _candidate_models(request)]
    selected = min(
        evaluations,
        key=lambda item: (
            item.rmse,
            item.mae,
            item.model.name,
            tuple(item.model.parameters.values()),
        ),
    )

    history = [
        Observation(point.date, point.quantity_sold)
        for point in request.points
        if point.is_observed_demand
    ]
    first_forecast_date = request.points[-1].date + timedelta(days=1)
    forecasts: list[dict[str, Any]] = []
    interval_radius = 1.96 * selected.rmse
    for offset in range(request.forecast_days):
        forecast_date = first_forecast_date + timedelta(days=offset)
        quantity = max(0.0, selected.model.predict_one(history, forecast_date))
        lower_bound = max(0.0, quantity - interval_radius)
        upper_bound = max(quantity, quantity + interval_radius)
        forecasts.append(
            {
                "date": forecast_date.isoformat(),
                "quantity": round(quantity, 4),
                "lowerBound": round(lower_bound, 4),
                "upperBound": round(upper_bound, 4),
            }
        )
        history.append(Observation(forecast_date, quantity))

    training_points = [point for point in request.points if point.dataset_split == "Training"]
    validation_points = [point for point in request.points if point.dataset_split == "Validation"]
    return {
        "productId": request.product_id,
        "warehouseId": request.warehouse_id,
        "model": {
            "name": selected.model.name,
            "version": selected.model.version,
            "parameters": selected.model.parameters,
        },
        "trainingStartDate": training_points[0].date.isoformat(),
        "trainingEndDate": training_points[-1].date.isoformat(),
        "validationStartDate": validation_points[0].date.isoformat(),
        "validationEndDate": validation_points[-1].date.isoformat(),
        "forecastStartDate": first_forecast_date.isoformat(),
        "forecastEndDate": forecasts[-1]["date"],
        "mae": round(selected.mae, 4),
        "rmse": round(selected.rmse, 4),
        "evaluatedValidationPoints": selected.evaluated_points,
        "excludedPointCount": sum(not point.is_observed_demand for point in request.points),
        "candidates": [
            {
                "name": item.model.name,
                "version": item.model.version,
                "parameters": item.model.parameters,
                "mae": round(item.mae, 4),
                "rmse": round(item.rmse, 4),
                "evaluatedValidationPoints": item.evaluated_points,
            }
            for item in evaluations
        ],
        "forecasts": forecasts,
    }
