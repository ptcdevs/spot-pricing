#!make

encrypt:
	sops --encrypt --output launchSettings.sops.json launchSettings.json

decrypt:
	sops --decrypt --output launchSettings.json launchSettings.sops.json
