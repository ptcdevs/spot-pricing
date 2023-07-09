#!make

params/regions.json:
	aws ec2 describe-regions --region us-east-1 --all-regions --no-cli-pager > params/regions.json

params/instance-types.json:
	aws ec2 describe-instance-types --no-cli-pager > params/instance-types.json

avail-zones-us-east-2:
	aws ec2 describe-availability-zones --region us-east-2