#!make

get-regions:
	aws ec2 describe-regions --region us-east-1 --all-regions --no-cli-pager > params/regions.json

#see
availability-zones:
	aws ec2 describe-availability-zones