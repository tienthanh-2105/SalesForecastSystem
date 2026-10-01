from __future__ import annotations

import math
import unittest

from training.metrics import mean_absolute_error, root_mean_squared_error


class MetricTests(unittest.TestCase):
    def test_mae_uses_absolute_error_average(self) -> None:
        self.assertAlmostEqual(2.0, mean_absolute_error([1, 2, 3], [2, 4, 6]))

    def test_rmse_uses_square_root_of_mean_squared_error(self) -> None:
        self.assertAlmostEqual(math.sqrt(14 / 3), root_mean_squared_error([1, 2, 3], [2, 4, 6]))

    def test_metrics_reject_empty_or_mismatched_values(self) -> None:
        for function in (mean_absolute_error, root_mean_squared_error):
            with self.assertRaises(ValueError):
                function([], [])
            with self.assertRaises(ValueError):
                function([1], [1, 2])


if __name__ == "__main__":
    unittest.main()
