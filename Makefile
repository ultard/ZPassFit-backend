IMAGE ?= zpassfit-backend
TAG ?= latest
DOCKERFILE := ZPassFit/Dockerfile
BENCHMARK_PROJ := ZPassFit.Benchmarks/ZPassFit.Benchmarks.csproj
# Extra args after `--` for BenchmarkDotNet, e.g. `make benchmark BDN_ARGS='--filter *Jwt*'`
BDN_ARGS ?=

.PHONY: docker-build benchmark benchmark-quick

docker-build:
	docker build -f $(DOCKERFILE) -t $(IMAGE):$(TAG) .

benchmark:
	dotnet run -c Release --project $(BENCHMARK_PROJ) -- $(BDN_ARGS)

benchmark-quick:
	dotnet run -c Release --project $(BENCHMARK_PROJ) -- --job short $(BDN_ARGS)
