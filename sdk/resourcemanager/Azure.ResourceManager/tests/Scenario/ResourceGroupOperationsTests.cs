﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core.TestFramework;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using NUnit.Framework;

namespace Azure.ResourceManager.Tests
{
    public class ResourceGroupOperationsTests : ResourceManagerTestBase
    {
        public ResourceGroupOperationsTests(bool isAsync)
            : base(isAsync)//, RecordedTestMode.Record)
        {
        }

        [RecordedTest]
        [SyncOnly]
        public void NoDataValidation()
        {
            ////subscriptions/db1ab6f0-4769-4b27-930e-01e2ef9c123c/resourceGroups/myRg
            var resource = Client.GetResourceGroup($"/subscriptions/{Guid.NewGuid()}/resourceGroups/fakeRg");
            Assert.Throws<InvalidOperationException>(() => { var data = resource.Data; });
        }

        [TestCase]
        [RecordedTest]
        public async Task DeleteRg()
        {
            var rgOp = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg = rgOp.Value;
            await rg.DeleteAsync();
        }

        [TestCase]
        [RecordedTest]
        public async Task StartDeleteRg()
        {
            var rgOp = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg = rgOp.Value;
            var deleteOp = await rg.DeleteAsync(false);
            var response = deleteOp.GetRawResponse();
            Assert.AreEqual(202, response.Status);
            await deleteOp.UpdateStatusAsync();
            await deleteOp.WaitForCompletionResponseAsync();
            await deleteOp.WaitForCompletionResponseAsync(TimeSpan.FromSeconds(2));

            var rgOp2 = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg2 = rgOp.Value;
            rg2.Id.Name = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg2.DeleteAsync(false));
        }

        [TestCase]
        [RecordedTest]
        public void StartDeleteNonExistantRg()
        {
            var rgOp = InstrumentClientExtension(Client.GetResourceGroup($"/subscriptions/{TestEnvironment.SubscriptionId}/resourceGroups/fake"));
            var deleteOpTask = rgOp.DeleteAsync(false);
            RequestFailedException exception = Assert.ThrowsAsync<RequestFailedException>(async () => await deleteOpTask);
            Assert.AreEqual(404, exception.Status);
        }

        [TestCase]
        [RecordedTest]
        public async Task Get()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            ResourceGroup rg2 = await rg1.GetAsync();
            Assert.AreEqual(rg1.Data.Name, rg2.Data.Name);
            Assert.AreEqual(rg1.Data.Id, rg2.Data.Id);
            Assert.AreEqual(rg1.Data.Type, rg2.Data.Type);
            Assert.AreEqual(rg1.Data.Properties.ProvisioningState, rg2.Data.Properties.ProvisioningState);
            Assert.AreEqual(rg1.Data.Location, rg2.Data.Location);
            Assert.AreEqual(rg1.Data.ManagedBy, rg2.Data.ManagedBy);
            Assert.AreEqual(rg1.Data.Tags, rg2.Data.Tags);

            rg1.Id.Name = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.GetAsync());

            var ex = Assert.ThrowsAsync<RequestFailedException>(async () => await Client.GetResourceGroup(rg1.Data.Id + "x").GetAsync());
            Assert.AreEqual(404, ex.Status);
        }

        [TestCase]
        [RecordedTest]
        public async Task Update()
        {
            var rgName = Recording.GenerateAssetName("testrg");
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(rgName, new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            var parameters = new ResourceGroupPatchable
            {
                Name = rgName
            };
            ResourceGroup rg2 = await rg1.UpdateAsync(parameters);
            Assert.AreEqual(rg1.Data.Name, rg2.Data.Name);
            Assert.AreEqual(rg1.Data.Id, rg2.Data.Id);
            Assert.AreEqual(rg1.Data.Type, rg2.Data.Type);
            Assert.AreEqual(rg1.Data.Properties.ProvisioningState, rg2.Data.Properties.ProvisioningState);
            Assert.AreEqual(rg1.Data.Location, rg2.Data.Location);
            Assert.AreEqual(rg1.Data.ManagedBy, rg2.Data.ManagedBy);
            Assert.AreEqual(rg1.Data.Tags, rg2.Data.Tags);

            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.UpdateAsync(null));

            rg1.Id.Name = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.UpdateAsync(parameters));
        }

        [TestCase]
        [RecordedTest]
        public async Task StartExportTemplate()
        {
            var rgOp = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg = rgOp.Value;
            var parameters = new ExportTemplateRequest();
            parameters.Resources.Add("*");
            var expOp = await rg.ExportTemplateAsync(parameters, false);
            await expOp.WaitForCompletionAsync();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var expOp = await rg.ExportTemplateAsync(null, false);
                _ = await expOp.WaitForCompletionAsync();
            });

            rg.Id.Name = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg.ExportTemplateAsync(parameters, false));
        }

        [TestCase]
        [RecordedTest]
        public async Task AddTag()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            Assert.AreEqual(0, rg1.Data.Tags.Count);
            ResourceGroup rg2 = await rg1.AddTagAsync("key", "value");
            Assert.AreEqual(1, rg2.Data.Tags.Count);
            Assert.IsTrue(rg2.Data.Tags.Contains(new KeyValuePair<string, string>("key", "value")));
            Assert.AreEqual(rg1.Data.Name, rg2.Data.Name);
            Assert.AreEqual(rg1.Data.Id, rg2.Data.Id);
            Assert.AreEqual(rg1.Data.Type, rg2.Data.Type);
            Assert.AreEqual(rg1.Data.Properties.ProvisioningState, rg2.Data.Properties.ProvisioningState);
            Assert.AreEqual(rg1.Data.Location, rg2.Data.Location);
            Assert.AreEqual(rg1.Data.ManagedBy, rg2.Data.ManagedBy);

            Assert.ThrowsAsync<ArgumentException>(async () => _ = await rg1.AddTagAsync(null, "value"));
            Assert.ThrowsAsync<ArgumentException>(async () => _ = await rg1.AddTagAsync(" ", "value"));
        }

        [TestCase]
        [RecordedTest]
        public async Task SetTags()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            Assert.AreEqual(0, rg1.Data.Tags.Count);
            var tags = new Dictionary<string, string>()
            {
                { "key", "value"}
            };
            ResourceGroup rg2 = await rg1.SetTagsAsync(tags);
            Assert.AreEqual(tags, rg2.Data.Tags);
            Assert.AreEqual(rg1.Data.Name, rg2.Data.Name);
            Assert.AreEqual(rg1.Data.Id, rg2.Data.Id);
            Assert.AreEqual(rg1.Data.Type, rg2.Data.Type);
            Assert.AreEqual(rg1.Data.Properties.ProvisioningState, rg2.Data.Properties.ProvisioningState);
            Assert.AreEqual(rg1.Data.Location, rg2.Data.Location);
            Assert.AreEqual(rg1.Data.ManagedBy, rg2.Data.ManagedBy);

            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.SetTagsAsync(null));
        }

        [TestCase]
        [RecordedTest]
        public async Task RemoveTag()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            var tags = new Dictionary<string, string>()
            {
                { "k1", "v1"},
                { "k2", "v2"}
            };
            rg1 = await rg1.SetTagsAsync(tags);
            ResourceGroup rg2 = await rg1.RemoveTagAsync("k1");
            var tags2 = new Dictionary<string, string>()
            {
                { "k2", "v2"}
            };
            Assert.AreEqual(tags2, rg2.Data.Tags);
            Assert.AreEqual(rg1.Data.Name, rg2.Data.Name);
            Assert.AreEqual(rg1.Data.Id, rg2.Data.Id);
            Assert.AreEqual(rg1.Data.Type, rg2.Data.Type);
            Assert.AreEqual(rg1.Data.Properties.ProvisioningState, rg2.Data.Properties.ProvisioningState);
            Assert.AreEqual(rg1.Data.Location, rg2.Data.Location);
            Assert.AreEqual(rg1.Data.ManagedBy, rg2.Data.ManagedBy);

            Assert.ThrowsAsync<ArgumentException>(async () => _ = await rg1.RemoveTagAsync(null));
            Assert.ThrowsAsync<ArgumentException>(async () => _ = await rg1.RemoveTagAsync(" "));
        }

        [TestCase]
        [RecordedTest]
        public async Task ListAvailableLocations()
        {
            var rgOp = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg = rgOp.Value;
            var locations = await rg.GetAvailableLocationsAsync();
            int count = 0;
            foreach (var location in locations)
            {
                count++;
            }
            Assert.GreaterOrEqual(count, 1);
        }

        [TestCase]
        [RecordedTest]
        public async Task MoveResources()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            var rg2Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            ResourceGroup rg2 = rg2Op.Value;
            var genericResources = Client.DefaultSubscription.GetGenericResources();
            var aset = await CreateGenericAvailabilitySetAsync(rg1.Id);

            int countRg1 = await GetResourceCountAsync(genericResources, rg1);
            int countRg2 = await GetResourceCountAsync(genericResources, rg2);
            Assert.AreEqual(1, countRg1);
            Assert.AreEqual(0, countRg2);

            var moveInfo = new ResourcesMoveInfo();
            moveInfo.TargetResourceGroup = rg2.Id;
            moveInfo.Resources.Add(aset.Id);
            _ = await rg1.MoveResourcesAsync(moveInfo);

            countRg1 = await GetResourceCountAsync(genericResources, rg1);
            countRg2 = await GetResourceCountAsync(genericResources, rg2);
            Assert.AreEqual(0, countRg1);
            Assert.AreEqual(1, countRg2);

            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.MoveResourcesAsync(null));
        }

        [TestCase]
        [RecordedTest]
        public async Task StartMoveResources()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            var rg2Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            ResourceGroup rg2 = rg2Op.Value;
            var genericResources = Client.DefaultSubscription.GetGenericResources();
            var asetOp = await StartCreateGenericAvailabilitySetAsync(rg1.Id);
            GenericResource aset = await asetOp.WaitForCompletionAsync();

            int countRg1 = await GetResourceCountAsync(genericResources, rg1);
            int countRg2 = await GetResourceCountAsync(genericResources, rg2);
            Assert.AreEqual(1, countRg1);
            Assert.AreEqual(0, countRg2);

            var moveInfo = new ResourcesMoveInfo();
            moveInfo.TargetResourceGroup = rg2.Id;
            moveInfo.Resources.Add(aset.Id);
            var moveOp = await rg1.MoveResourcesAsync(moveInfo, false);
            _ = await moveOp.WaitForCompletionResponseAsync();

            countRg1 = await GetResourceCountAsync(genericResources, rg1);
            countRg2 = await GetResourceCountAsync(genericResources, rg2);
            Assert.AreEqual(0, countRg1);
            Assert.AreEqual(1, countRg2);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var moveOp = await rg1.MoveResourcesAsync(null, false);
                _ = await moveOp.WaitForCompletionResponseAsync();
            });
        }

        [TestCase]
        [RecordedTest]
        public async Task ValidateMoveResources()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            var rg2Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            ResourceGroup rg2 = rg2Op.Value;
            var aset = await CreateGenericAvailabilitySetAsync(rg1.Id);

            var moveInfo = new ResourcesMoveInfo();
            moveInfo.TargetResourceGroup = rg2.Id;
            moveInfo.Resources.Add(aset.Id);
            var validateOp = await rg1.ValidateMoveResourcesAsync(moveInfo);
            Response response = validateOp.GetRawResponse();

            Assert.AreEqual(204, response.Status);

            Assert.ThrowsAsync<ArgumentNullException>(async () => _ = await rg1.ValidateMoveResourcesAsync(null));
        }

        [TestCase]
        [RecordedTest]
        public async Task StartValidateMoveResources()
        {
            var rg1Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            var rg2Op = await Client.DefaultSubscription.GetResourceGroups().CreateOrUpdateAsync(Recording.GenerateAssetName("testrg"), new ResourceGroupData(Location.WestUS2));
            ResourceGroup rg1 = rg1Op.Value;
            ResourceGroup rg2 = rg2Op.Value;
            var asetOp = await StartCreateGenericAvailabilitySetAsync(rg1.Id);
            GenericResource aset = await asetOp.WaitForCompletionAsync();

            var moveInfo = new ResourcesMoveInfo();
            moveInfo.TargetResourceGroup = rg2.Id;
            moveInfo.Resources.Add(aset.Id);
            var validateOp = await rg1.ValidateMoveResourcesAsync(moveInfo, false);
            Response response = await validateOp.WaitForCompletionResponseAsync();

            Assert.AreEqual(204, response.Status);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var moveOp = await rg1.ValidateMoveResourcesAsync(null, false);
                _ = await moveOp.WaitForCompletionResponseAsync();
            });
        }

        [TestCase]
        [Ignore("4622 needs complete with a Mocked example to fill in this test")]
        public void CreateResourceFromModel()
        {
            //public ArmResponse<TOperations> CreateResource<TContainer, TOperations, TResource>(string name, TResource model, azure_proto_core.Location location = default)
        }

        [TestCase]
        [Ignore("4622 needs complete with a Mocked example to fill in this test")]
        public void CreateResourceFromModelAsync()
        {
            //public ArmResponse<TOperations> CreateResource<TContainer, TOperations, TResource>(string name, TResource model, azure_proto_core.Location location = default)
        }
    }
}
