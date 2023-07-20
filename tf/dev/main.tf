terraform {
  cloud {
    organization = "ptcdevs"

    workspaces {
      name = "infra-common"
    }
  }
}
