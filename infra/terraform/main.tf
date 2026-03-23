terraform {
  required_version = ">= 1.5.0"
}

provider "azurerm" {
  features {}
}

# Example skeleton only – to be customized
resource "azurerm_resource_group" "cephasops" {
  name     = "rg-cephasops"
  location = "southeastasia"
}
