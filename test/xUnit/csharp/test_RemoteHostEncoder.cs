// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Remoting;
using Xunit;

namespace PSTests.Parallel
{
    public static class RemoteHostEncoderTests
    {
        public static TheoryData<IEnumerable<int>> MultipleChoiceDefaultChoices => new()
        {
            new int[] { 0, 2 },
            Array.Empty<int>(),
            new Collection<int> { 0, 2 },
            new Collection<int>(),
        };

        [Theory]
        [MemberData(nameof(MultipleChoiceDefaultChoices))]
        public static void PromptForChoiceMultipleSelectionDefaultChoicesRoundTrip(IEnumerable<int> defaultChoices)
        {
            // Older servers pass the defaultChoices value through as is so the
            // client needs to be able to decode both the array and collection
            // wire formats.
            Collection<ChoiceDescription> choices = new()
            {
                new ChoiceDescription("&a"),
                new ChoiceDescription("&b"),
                new ChoiceDescription("&c"),
            };
            RemoteHostCall call = new(
                1,
                RemoteHostMethodId.PromptForChoiceMultipleSelection,
                new object[] { "caption", "message", choices, defaultChoices });

            RemoteHostCall decoded = RemoteHostCall.Decode(SerializeForRemoting(call.Encode()));

            Assert.Equal(RemoteHostMethodId.PromptForChoiceMultipleSelection, decoded.MethodId);
            IEnumerable<int> actual = Assert.IsAssignableFrom<IEnumerable<int>>(decoded.Parameters[3]);
            Assert.Equal(defaultChoices.ToArray(), actual.ToArray());
        }

        [Fact]
        public static void DecodeEnumerableOfIntWithUnknownFormatFails()
        {
            PSObject data = new();
            data.Properties.Add(new PSNoteProperty("Foo", "Bar"));

            PSRemotingDataStructureException exc = Assert.Throws<PSRemotingDataStructureException>(
                () => RemoteHostEncoder.DecodeObject(data, typeof(IEnumerable<int>)));
            Assert.Equal(
                string.Format(RemotingErrorIdStrings.RemoteHostDataDecodingNotSupported, typeof(IEnumerable<int>)),
                exc.Message);
        }

        private static PSObject SerializeForRemoting(PSObject data)
        {
            Fragmentor fragmentor = new(32 * 1024, null);
            using MemoryStream stream = new();
            fragmentor.SerializeToBytes(data, stream);
            stream.Position = 0;

            return fragmentor.DeserializeToPSObject(stream);
        }
    }
}
