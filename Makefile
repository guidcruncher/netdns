SOLUTION = DnsForwarder.sln
PROJECT = src/DnsForwarder/DnsForwarder.csproj
TESTSDNS = tests/DnsForwarder.Dns.Tests/DnsForwarder.Dns.Tests.csproj
TESTSDHCP = tests/DnsForwarder.Dhcp.Tests/DnsForwarder.Dhcp.Tests.csproj
TESTSNTP = tests/DnsForwarder.Ntp.Tests/DnsForwarder.Ntp.Tests.csproj
BUILD_DIR = bin/
RUNTIME = linux-x64

.PHONY: all build clean run test restore publish metrics dev benchmark format dig docs mkdocs-install

all: restore build

dig:
	dig itv.com @127.0.0.1 -p 1053

mkdocs-install:
	pip install mkdocs --break-system-packages
	pip install mkdocs-material --break-system-packages
	pip install mkdocs-mermaid2-plugin --break-system-packages

docs:
	mkdocs serve --dev-addr 0.0.0.0:8000 --config-file ./mkdocs.yml --watch ./docs

metrics:
	curl http://127.0.0.1:1080/metrics  -v

restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION) -c Release

format:
	dotnet format ${SOLUTION}

run:
	dotnet run --project $(PROJECT)

dev:
	dotnet run --project ${PROJECT} -c Debug -- --config appsettings.Development.json

benchmark:
	dotnet run -c Release --project tests/DnsForwarder.Benchmarks/DnsForwarder.Benchmarks.csproj

test:
	dotnet test $(TESTSDNS) -c Release --no-build
	dotnet test $(TESTSDHCP) -c Release --no-build
	dotnet test ${TESTSNTP} -c Release --no-build

clean:
	rm -rf $(BUILD_DIR)
	rm -rf ./BenchmarkDotNet.Artifacts
	rm -rf ./tests/DnsForwarder.Dhcp.Tests/bin
	rm -rf ./tests/DnsForwarder.Dns.Tests/bin
	rm -rf ./tests/DnsForwarder.Benchmarks/bin
	rm -rf ./tests/DnsForwarder.Ntp.Tests/bin
	rm -rf ./tests/DnsForwarder.Dhcp.Tests/obj
	rm -rf ./tests/DnsForwarder.Dns.Tests/obj
	rm -rf ./tests/DnsForwarder.Ntp.Tests/obj
	rm -rf ./tests/DnsForwarder.Benchmarks/obj
	dotnet clean $(SOLUTION)

publish:
	dotnet publish $(PROJECT) -c Release -r $(RUNTIME) --self-contained false -o publish/

docker-build:
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--progress=plain
		.

docker-run:
	docker compose -f ./docker-compose.yml down
	docker compose -f ./docker-compose.yml rm -f
	docker compose -f ./docker-compose.yml build --no-cache
	docker compose -f ./docker-compose.yml up -d
	docker compose -f ./docker-compose.yml logs -f

docker-run-dev:
	docker compose -f ./docker-compose-dev.yml down
	docker compose -f ./docker-compose-dev.yml rm -f
	docker compose -f ./docker-compose-dev.yml build --no-cache
	docker compose -f ./docker-compose-dev.yml up -d
	docker compose -f ./docker-compose-dev.yml logs -f

docker-stop:
	docker compose -f ./docker-compose.yml stop
	docker compose -f ./docker-compose.yml rm -f

publish: ## Build the Docker image
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/guidcruncher/dns-forwarder:latest \
		--progress=plain \
		--push \
		.
