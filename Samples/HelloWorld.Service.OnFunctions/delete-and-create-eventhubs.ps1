#  Copyright (c) Microsoft Corporation. All rights reserved.
#  Licensed under the MIT license.


# Replace these to match your project

$NameSpace = 'the-eventhubs-namespace'
$ResourceGroup = 'the-resource-group-containing-the-eventhubs-namespace'

# This deletes and then creates the eventhubs.

echo "Deleting existing EventHubs..."
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Doorbell
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group0
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group1
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group2
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group3
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group4
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group5
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group6
az eventhubs eventhub delete --namespace-name $NameSpace --resource-group $ResourceGroup --name Group7

echo "Creating fresh EventHubs..."
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Doorbell --message-retention 1 --partition-count 32
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group0 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group1 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group2 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group3 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group4 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group5 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group6 --message-retention 1 --partition-count 4
az eventhubs eventhub create --namespace-name $NameSpace --resource-group $ResourceGroup --name Group7 --message-retention 1 --partition-count 4

echo "Done."

# NOTE
#
# If clearing eventhubs as above, the functions' consumer positions, which are stored in blob storage, also need to be cleared. 
# The easy way to do this is to remove the entire blob folder
#
# (webjobs storage account)/azurewebjobs-eventhub/XXX.servicebus.windows.net
#
# where XXX is $NameSpace
