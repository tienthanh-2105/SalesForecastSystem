# SalesForecastSystem ML

Module Python này chịu trách nhiệm đánh giá và tạo dự báo nhu cầu. Backend ASP.NET Core chuẩn bị chuỗi dữ liệu ngày; module ML không truy cập SQL Server trực tiếp và trả về một kết quả có cấu trúc để backend lưu vào `ForecastModels`, `ForecastRuns` và `ForecastResults`.

## Mô hình hiện có

- Moving average với các cửa sổ 3, 7 và 14 ngày.
- Seasonal naive với chu kỳ 7 ngày.
- Walk-forward validation: mỗi điểm kiểm định chỉ sử dụng dữ liệu ở trước ngày cần dự báo.
- Chọn mô hình theo RMSE thấp nhất, sau đó dùng MAE làm tiêu chí phụ.
- Loại `StockOut` và `MissingInventoryHistory` khỏi tập quan sát vì giá trị bán bằng 0 trong các ngày này không đại diện chắc chắn cho nhu cầu bằng 0.

## Chạy dự báo

```powershell
python .\SalesForecastSystem.ML\main.py --input request.json --output result.json
```

Nếu bỏ `--output`, kết quả JSON được ghi ra standard output. Định dạng request mẫu:

```json
{
  "productId": 1,
  "warehouseId": 1,
  "forecastDays": 7,
  "points": [
    {"date": "2026-09-01", "quantitySold": 4, "dataStatus": "Sold", "datasetSplit": "Training"},
    {"date": "2026-09-02", "quantitySold": 0, "dataStatus": "NoSale", "datasetSplit": "Validation"}
  ]
}
```

## Kiểm thử

```powershell
python -m unittest discover -s .\SalesForecastSystem.ML\tests -v
```

Module chỉ sử dụng Python standard library để baseline có thể chạy ổn định mà không cần cài package ngoài. `requirements.txt` được giữ để bổ sung thư viện cho mô hình ML nâng cao sau này.
