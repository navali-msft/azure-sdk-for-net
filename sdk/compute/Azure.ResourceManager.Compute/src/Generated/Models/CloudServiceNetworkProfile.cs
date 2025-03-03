// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Collections.Generic;
using Azure.Core;

namespace Azure.ResourceManager.Compute.Models
{
    /// <summary> Network Profile for the cloud service. </summary>
    public partial class CloudServiceNetworkProfile
    {
        /// <summary> Initializes a new instance of CloudServiceNetworkProfile. </summary>
        public CloudServiceNetworkProfile()
        {
            LoadBalancerConfigurations = new ChangeTrackingList<LoadBalancerConfiguration>();
        }

        /// <summary> Initializes a new instance of CloudServiceNetworkProfile. </summary>
        /// <param name="loadBalancerConfigurations"> List of Load balancer configurations. Cloud service can have up to two load balancer configurations, corresponding to a Public Load Balancer and an Internal Load Balancer. </param>
        /// <param name="swappableCloudService"> The id reference of the cloud service containing the target IP with which the subject cloud service can perform a swap. This property cannot be updated once it is set. The swappable cloud service referred by this id must be present otherwise an error will be thrown. </param>
        internal CloudServiceNetworkProfile(IList<LoadBalancerConfiguration> loadBalancerConfigurations, SubResource swappableCloudService)
        {
            LoadBalancerConfigurations = loadBalancerConfigurations;
            SwappableCloudService = swappableCloudService;
        }

        /// <summary> List of Load balancer configurations. Cloud service can have up to two load balancer configurations, corresponding to a Public Load Balancer and an Internal Load Balancer. </summary>
        public IList<LoadBalancerConfiguration> LoadBalancerConfigurations { get; }
        /// <summary> The id reference of the cloud service containing the target IP with which the subject cloud service can perform a swap. This property cannot be updated once it is set. The swappable cloud service referred by this id must be present otherwise an error will be thrown. </summary>
        public SubResource SwappableCloudService { get; set; }
    }
}
