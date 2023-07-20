#!make

keygen:
	@echo "run \"age-keygen -o ~/.age\" to generate a new AGE file"

encrypt:
	ls *secret.env | sed -e 's/.env//g' | xargs -I{} sops --encrypt --output {}.sops.env {}.env
	ls *secret.yaml | sed -e 's/.yaml//g' | xargs -I{} sops --encrypt --output {}.sops.yaml {}.yaml

decrypt:
	ls *secret.sops.env | sed -e 's/.sops.env//g' | xargs -I{} sops --decrypt --output {}.env {}.sops.env
	ls *secret.sops.yaml | sed -e 's/.sops.yaml//g' | xargs -I{} sops --decrypt --output {}.yaml {}.sops.yaml