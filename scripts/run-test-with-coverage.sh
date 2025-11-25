# 1. Clean all standard build artifacts
dotnet clean

# 2. Manually and recursively find and delete ALL TestResults directories
# to ensure we don't find any old reports.
find . -type d -name "TestResults" -exec rm -rf {} +

# 3. Clean the top-level report directory
rm -rf coveragereport

# 4. Create a timestamp
timestamp=$(date +"%Y%m%d-%H%M%S")

# 5. Run the test command. We know from the logs this works.
dotnet test --configuration Debug --coverage --coverage-output-format cobertura

# 6. Generate the report using a wildcard (*) for the filename.
# This is the crucial fix that will find the GUID-named file.
reportgenerator "-reports:**/*.cobertura.xml" "-targetdir:coveragereport/$timestamp" -reporttypes:Html