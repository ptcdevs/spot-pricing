#!make

GITREF=`git rev-parse --abbrev-ref HEAD |  sed -e 's/\(.*\)/\L\1/'`

params/regions.json:
	aws ec2 describe-regions --region us-east-1 --all-regions --no-cli-pager > params/regions.json

params/instance-types.json:
	aws ec2 describe-instance-types --no-cli-pager > params/instance-types.json

avail-zones-us-east-2:
	aws ec2 describe-availability-zones --region us-east-2
	
params/gpu-instances.json: params/instance-types.json
	jq -r "[.InstanceTypes[] | \
 		select((.InstanceType|startswith(\"p4\")) \
			or (.InstanceType|startswith(\"p3\")) \
			or (.InstanceType|startswith(\"g3\")) \
			or (.InstanceType|startswith(\"g4\")) \
			or (.InstanceType|startswith(\"g5\")) \
			or (.InstanceType|startswith(\"g5g\"))) \
		| .InstanceType] |sort " params/instance-types.json > params/gpu-instances.json

csharp/aws-console/params/gpu-instances.json: params/gpu-instances.json
	cp params/gpu-instances.json csharp/aws-console/params/gpu-instances.json

gpu-instances: csharp/aws-console/params/gpu-instances.json

.PHONY: params/gpu-instances.json csharp/aws-console/params/gpu-instances.json

pricing-describe-services:
	aws pricing describe-services --region us-east-1 --service-code AmazonEC2 --format-version aws_v1 --max-items 1
	
#pricing-get-:
#	aws pricing describe-services --region us-east-1 --service-code AmazonEC2 --format-version aws_v1 --max-items 1
#	

docker-compose-up:
	docker-compose up 
	
ghcr-login:
	docker login ghcr.io --username vector623 --password ${GITHUB_TOKEN}

docker-build:
	docker buildx build -t ghcr.io/ptcdevs/spot-pricing:${GITREF} .

docker-push:
	docker push ghcr.io/ptcdevs/spot-pricing:${GITREF}

redeploy: docker-build docker-push
	cd helm; \
		make reinstall
	

echo:
	echo "GITREF: ${GITREF}"
	#ghcr.io/ptcdevs/spot-pricing:${{ needs.set-refs.outputs.branch }}