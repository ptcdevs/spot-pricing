#!make

keygen:
	@echo "run \"age-keygen -o ~/.age\" to generate a new AGE file"

encrypt:
	sops --encrypt --output launchSettings.sops.json launchSettings.json

decrypt:
	sops --decrypt --output launchSettings.json launchSettings.sops.json