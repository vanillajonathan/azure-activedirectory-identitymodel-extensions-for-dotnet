//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.TestUtils;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.Protocols.Tests
{
    /// <summary>
    /// An implementation of IConfigurationRetriever geared towards Azure AD issuers metadata.
    /// </summary>
    internal class IssuerConfigurationRetriever : IConfigurationRetriever<IssuerMetadata>
    {
        /// <summary>Retrieves a populated configuration given an address and an <see cref="T:Microsoft.IdentityModel.Protocols.IDocumentRetriever"/>.</summary>
        /// <param name="address">Address of the discovery document.</param>
        /// <param name="retriever">The <see cref="T:Microsoft.IdentityModel.Protocols.IDocumentRetriever"/> to use to read the discovery document.</param>
        /// <param name="cancel">A cancellation token that can be used by other objects or threads to receive notice of cancellation. <see cref="T:System.Threading.CancellationToken"/>.</param>
        /// <returns>
        /// A <see cref="Task{IssuerMetadata}"/> that, when completed, returns <see cref="IssuerMetadata"/> from the configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">address - Azure AD Issuer metadata address URL is required
        /// or retriever - No metadata document retriever is provided.</exception>
        public async Task<IssuerMetadata> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            string doc = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
            return new IssuerMetadata();
        }
    }

    /// <summary>
    /// Model class to hold information parsed from the Azure AD issuer endpoint.
    /// </summary>
    internal class IssuerMetadata
    {
        /// <summary>
        /// Issuer associated with the OIDC endpoint.
        /// </summary>
        public string Issuer { get; set; }
    }

    public class ExtensibilityTests
    {

        [Theory, MemberData(nameof(GetMetadataTheoryData))]
        public void GetMetadataTest(DocumentRetrieverTheoryData theoryData)
        {
            TestUtilities.WriteHeader($"{this}.GetMetadataTest", theoryData);
            try
            {
                string doc = theoryData.DocumentRetriever.GetDocumentAsync(theoryData.Address, CancellationToken.None).Result;
                Assert.NotNull(doc);
                theoryData.ExpectedException.ProcessNoException();
            }
            catch (AggregateException aex)
            {
                aex.Handle((x) =>
                {
                    theoryData.ExpectedException.ProcessException(x);
                    return true;
                });
            }
        }

        public static TheoryData<DocumentRetrieverTheoryData> GetMetadataTheoryData
        {
            get
            {
                var theoryData = new TheoryData<DocumentRetrieverTheoryData>();

                var documentRetriever = new FileDocumentRetriever();
                theoryData.Add(new DocumentRetrieverTheoryData
                {
                    Address = null,
                    DocumentRetriever = documentRetriever,
                    ExpectedException = ExpectedException.ArgumentNullException(),
                    First = true,
                    TestId = "Address NULL"
                });

                theoryData.Add(new DocumentRetrieverTheoryData
                {
                    Address = "OpenIdConnectMetadata.json",
                    DocumentRetriever = documentRetriever,
                    ExpectedException = ExpectedException.IOException("IDX20804:", typeof(FileNotFoundException), "IDX20814:"),
                    TestId = "File not found: OpenIdConnectMetadata.json"
                });

                theoryData.Add(new DocumentRetrieverTheoryData
                {
                    Address = "ValidJson.json",
                    DocumentRetriever = documentRetriever,
                    TestId = "ValidJson.json - JsonWebKeySet"
                });

                return theoryData;
            }
        }
    }
}

#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
