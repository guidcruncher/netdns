SOLUTION = DnsForwarder.sln
PROJECT = src/DnsForwarder/DnsForwarder.csproj
TESTS = tests/DnsForwarder.Tests/DnsForwarder.Tests.csproj
TESTSDHCP = tests/DnsForwarder.Dhcp.Tests/DnsForwarder.Dhcp.Tests.csproj
BUILD_DIR = bin/
RUNTIME = linux-x64

.PHONY: all build clean run test restore publish dev benchmark format dig

all: restore build

dig:
	dig itv.com @127.0.0.1 -p 1053

restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION) -c Release

format:
	dotnet format ${SOLUTION}

run:
	dotnet run --project $(PROJECT)

dev:
	dotnet run --project ${PROJECT} -- --config appsettings.Development.json

benchmark:
	dotnet run -c Release --project tests/DnsForwarder.Benchmarks/DnsForwarder.Benchmarks.csproj

test:
	dotnet test $(TESTS) -c Release --no-build
	dotnet test $(TESTSDHCP) -c Release --no-build

clean:
	rm -rf $(BUILD_DIR)
	rm -rf ./BenchmarkDotNet.Artifacts
	rm -rf ./tests/DnsForwarder.Dhcp.Tests/bin
	rm -rf ./tests/DnsForwarder.Dns.Tests/bin
	rm -rf ./tests/DnsForwarder.Benchmarks/bin
	rm -rf ./tests/DnsForwarder.Dhcp.Tests/obj
	rm -rf ./tests/DnsForwarder.Dns.Tests/obj
	rm -rf ./tests/DnsForwarder.Benchmarks/obj
	dotnet clean $(SOLUTION)

publish:
	dotnet publish $(PROJECT) -c Release -r $(RUNTIME) --self-contained false -o publish/

docker-build:
	docker build -t dns-forwarder .

docker-run:
	docker run --rm -p 53:53/udp dns-forwarder
