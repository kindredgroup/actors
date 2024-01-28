_help:
    @just --list

# runs the example app
run *ARGS:
    dotnet run --project Examples/Examples.csproj {{ARGS}}

# runs the test cases
test:
    dotnet test

# loops through test cases
loop:
    while [ true ]; do dotnet test; done
