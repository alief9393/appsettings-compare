set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SERVER_PROJECT="$ROOT/src/server-demo"
COMPARE_PROJECT="$ROOT/src/CompareAppSettings"
LOCAL_APPSET="$COMPARE_PROJECT/appsettings.json"
SERVER_URL="http://localhost:5000/appsettings"

echo "Building server"
dotnet build "$SERVER_PROJECT/server-demo.csproj" -c Release

echo "Building compare tool"
dotnet build "$COMPARE_PROJECT/CompareAppSettings.csproj" -c Release

echo "Starting server"
dotnet run --project "$SERVER_PROJECT" --urls "http://localhost:5000" &
SERVER_PID=$!
echo "Server PID: $SERVER_PID"
sleep 2

echo "Running compare (local vs server)..."
set +e
dotnet run --project "$COMPARE_PROJECT" -- --local "$LOCAL_APPSET" --remote-url "$SERVER_URL"
COMPARE_EXIT=$?
set -e

echo "Stopping server"
kill "$SERVER_PID" || true
wait "$SERVER_PID" 2>/dev/null || true

if [ "$COMPARE_EXIT" -eq 0 ]; then
  echo "AppSettings are the SAME. CD passes."
  exit 0
else
  echo "AppSettings differ. CD fails. Exit code: $COMPARE_EXIT"
  exit 1
fi
