IMAGE ?= zpassfit-backend
TAG ?= latest
DOCKERFILE := ZPassFit/Dockerfile
BENCHMARK_PROJ := ZPassFit.Benchmarks/ZPassFit.Benchmarks.csproj
YOOKASSA_PROJ := ZPassFit.YooKassa/ZPassFit.YooKassa.csproj

YOOKASSA_OPENAPI ?= ZPassFit.YooKassa/openapi.yaml
YOOKASSA_KIOTA_OUT := ZPassFit.YooKassa/Generated
# Extra args after `--` for BenchmarkDotNet, e.g. `make benchmark BDN_ARGS='--filter *Jwt*'`
BDN_ARGS ?=

.PHONY: docker-build benchmark benchmark-quick kiota-yookassa

docker-build:
	docker build -f $(DOCKERFILE) -t $(IMAGE):$(TAG) .

benchmark:
	dotnet run -c Release --project $(BENCHMARK_PROJ) -- $(BDN_ARGS)

benchmark-quick:
	dotnet run -c Release --project $(BENCHMARK_PROJ) -- --job short $(BDN_ARGS)

kiota-yookassa:
	dotnet tool restore
	dotnet kiota generate --language CSharp \
		--openapi $(YOOKASSA_OPENAPI) \
		--output $(YOOKASSA_KIOTA_OUT) \
		--namespace-name ZPassFit.YooKassa \
		--class-name YooKassaApiClient \
		--clean-output \
		--disable-validation-rules All
