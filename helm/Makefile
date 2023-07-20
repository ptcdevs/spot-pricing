#!make

K8S_NAMESPACE="spot-pricing-dev"
GITREF=`git rev-parse --abbrev-ref HEAD |  sed -e 's/\(.*\)/\L\1/'`

template:
	helm template ./spot-pricing \
		--set image.tag=${GITREF} \
		--set ingress.hosts.0.host=${GITREF} \
		--set ingress.hosts.0.paths.0.path="/" \
		--set ingress.hosts.0.paths.0.pathType=ImplementationSpecific
