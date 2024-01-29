_help:
    @just --list

# runs the example app
run *ARGS:
    dotnet run --project Examples {{ARGS}}

# runs the test cases
test:
    dotnet test

# loops through test cases
loop:
    while [ true ]; do dotnet test; done

# runs the benchmarks
bench:
    dotnet run -c Release --project Actors.Benchmarks
