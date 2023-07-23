#!make

K8S_NAMESPACE="spot-pricing-dev"
GITREF=`git rev-parse --abbrev-ref HEAD |  sed -e 's/\(.*\)/\L\1/'`

gitref:
	echo "GITREF: ${GITREF}"

template:
	helm template ./spot-pricing \
		--set image.tag=${GITREF} \
		--set ingress.hosts.0.host=${GITREF} \
		--set ingress.hosts.0.paths.0.path="/" \
		--set ingress.hosts.0.paths.0.pathType=ImplementationSpecific

install:
	helm -n spot-pricing-dev install spot-pricing ./spot-pricing \
		--set image.tag=${GITREF} \
		--wait

uninstall:
	-helm -n spot-pricing-dev uninstall spot-pricing

reinstall: uninstall install

check-dev-cert:
	openssl s_client -showcerts -connect spot-pricing.dev.xounges.net:443 -servername spot-pricing.dev.xounges.net