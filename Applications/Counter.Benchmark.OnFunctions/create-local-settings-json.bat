#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the MIT license.

# This copies the Azure Functions application settings out of the portal, 
# creating a local.settings.json that uses the exact same configuration

func azure functionapp fetch-app-settings %FunctionsApp%

# NOTE: after calling this for the first time, manually change the IsEncrypted field 
# inside the generated host.json to false (if necessary) and then run the above command again
