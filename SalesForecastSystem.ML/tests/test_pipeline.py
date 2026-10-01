from __future__ import annotations

import unittest

from training.contracts import ForecastRequest
from training.pipeline import run_forecast


def request_payload() -> dict:
    quantities = [2, 4, 6, 8, 10, 12, 14, 2, 4, 6]
    return {
        "productId": 9,
        "warehouseId": 3,
        "forecastDays": 3,
        "movingAverageWindows": [3],
        "seasonalPeriods": [7],
        "points": [
            {
                "date": f"2026-09-{index + 1:02d}",
                "quantitySold": quantity,
                "dataStatus": "Sold" if quantity else "NoSale",
                "datasetSplit": "Training" if index < 7 else "Validation",
            }
            for index, quantity in enumerate(quantities)
        ],
    }


class PipelineTests(unittest.TestCase):
    def test_selects_lowest_rmse_and_returns_non_negative_forecast(self) -> None:
        result = run_forecast(ForecastRequest.from_dict(request_payload()))

        self.assertEqual("SeasonalNaive", result["model"]["name"])
        self.assertEqual(0.0, result["mae"])
        self.assertEqual(0.0, result["rmse"])
        self.assertEqual(3, len(result["forecasts"]))
        self.assertEqual("2026-09-11", result["forecastStartDate"])
        self.assertTrue(all(item["quantity"] >= 0 for item in result["forecasts"]))

    def test_stockout_is_excluded_from_metric_and_training_history(self) -> None:
        payload = request_payload()
        payload["points"][7]["quantitySold"] = 0
        payload["points"][7]["dataStatus"] = "StockOut"
        result = run_forecast(ForecastRequest.from_dict(payload))

        self.assertEqual(1, result["excludedPointCount"])
        self.assertEqual(2, result["evaluatedValidationPoints"])

    def test_rejects_non_continuous_or_invalid_split(self) -> None:
        payload = request_payload()
        payload["points"][1]["date"] = "2026-09-04"
        with self.assertRaisesRegex(ValueError, "strictly ordered|continuous"):
            ForecastRequest.from_dict(payload)

        payload = request_payload()
        payload["points"][0]["datasetSplit"] = "Unknown"
        with self.assertRaisesRegex(ValueError, "unsupported datasetSplit"):
            ForecastRequest.from_dict(payload)


if __name__ == "__main__":
    unittest.main()
