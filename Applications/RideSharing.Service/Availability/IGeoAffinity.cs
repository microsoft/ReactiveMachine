// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing
{
    // define key dimensions
    public interface IGeoAffinity : 
        IPartitionedAffinity<IGeoAffinity,GeoLocation>
    {
        GeoLocation Location { get; }
    }

    public struct GeoLocation : IEquatable<GeoLocation>
    {
        public GeoLocation Location => this;

        // for this simple sample, we just use zipcode
        public int ZipCode;

        public ulong GetHashInput()
        {
            return (ulong) ZipCode;
        }

        public GeoLocation(int zipCode)
        {
            this.ZipCode = zipCode;
        }

        public IEnumerable<GeoLocation> GetNearbyAreas()
        {
            foreach(var zipCode in ZipCodes.GetProximityList(ZipCode))
            {
                yield return new GeoLocation(zipCode);
            }
        }

        public bool Equals(GeoLocation other)
        {
            return Equals(ZipCode, other.ZipCode);
        }

    }

    public static class ZipCodes
    {
        public static List<int> All = new List<int> {
                98101, 98102, 98104, 98105, 98108, 98109, 98112, 98113, 98114, 98117, 98103, 98106, 98107,
                98111, 98115, 98116, 98118, 98119, 98121, 98125, 98126, 98132, 98133, 98138, 98139, 98141,
                98122, 98124, 98127, 98129, 98131, 98134, 98136, 98144, 98145, 98148, 98155, 98160, 98161,
                98164, 98165, 98168, 98170, 98146, 98154, 98158, 98166, 98174, 98175, 98178, 98190, 98191,
                98177, 98181, 98185, 98188, 98189, 98194, 98195, 98199, 98198
        };


        // simulate proximity
        public static IEnumerable<int> GetProximityList(int zipCode)
        {
            var position = All.IndexOf(zipCode);

            yield return All[position];
            if (position + 1 < All.Count) yield return All[position + 1];
            if (position - 1 > 0) yield return All[position - 1];
            if (position + 2 < All.Count) yield return All[position + 2];
            if (position - 2 > 0) yield return All[position - 2];
        }
    }
}
