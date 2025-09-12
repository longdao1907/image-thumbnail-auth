#!/usr/bin/env sh
set -e

# --- Cấu hình ứng dụng ---
PORT="${PORT:-8080}"
# Lưu ý: Nếu không cấu hình SSL cho Kestrel, hãy dùng http
export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
#export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

# --- Cấu hình Cloud SQL Proxy ---
# SỬA LỖI 1: Sửa lại cú pháp gán biến và dùng ':-' cho giá trị mặc định.
CLOUDSQL_INSTANCE="${CLOUDSQL_INSTANCE:-durable-sky-471008-q2:us-central1:my-first-postgresql}"

# # Cổng proxy local cho Postgres
DB_PROXY_PORT="${DB_PROXY_PORT:-5432}"

# # Thêm flag tuỳ chọn cho proxy, ví dụ: --auto-iam-authn, --private-ip
PROXY_EXTRA_ARGS="${PROXY_EXTRA_ARGS:-}"

# # --- Khởi động Proxy ---
echo "[entrypoint] Starting Cloud SQL Proxy for ${CLOUDSQL_INSTANCE} on 127.0.0.1:${DB_PROXY_PORT}"

# # Chạy proxy ở chế độ nền (background)
# # Lưu ý: Các tham số tuỳ chọn phải được đặt trước tên instance
/usr/local/bin/cloud-sql-proxy --structured-logs --port="${DB_PROXY_PORT}" ${PROXY_EXTRA_ARGS} "${CLOUDSQL_INSTANCE}" &
PROXY_PID=$!

# # --- Quản lý tiến trình ---
# # Hàm dọn dẹp khi nhận tín hiệu dừng
term_handler() {
  echo "[entrypoint] Termination signal caught, stopping proxy..."
  # Gửi tín hiệu dừng đến proxy
  kill -TERM "${PROXY_PID}" 2>/dev/null
  # Chờ proxy dừng hoàn toàn
  wait "${PROXY_PID}" 2>/dev/null
  echo "[entrypoint] Proxy stopped."
}

# # Bẫy tín hiệu TERM và INT để gọi hàm term_handler
# # Khi container dừng, nó sẽ dọn dẹp proxy một cách gọn gàng.
trap 'term_handler' TERM INT

# --- Khởi động ứng dụng chính ---
echo "[entrypoint] Starting .NET application..."
# SỬA LỖI 2: Chạy ứng dụng .NET ở chế độ tiền cảnh (foreground) bằng 'exec'.
# Tiến trình này sẽ trở thành PID 1 của container.
# Khi ứng dụng này dừng, container sẽ dừng, và trap ở trên sẽ được kích hoạt để dọn dẹp proxy.
exec dotnet AuthAPI.dll

# Các dòng 'wait' và 'kill' ở cuối script cũ đã được loại bỏ vì 'trap' và 'exec' đã xử lý logic này.
	