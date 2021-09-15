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
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IdentityModel.Tokens.Jwt.Tests;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Microsoft.IdentityModel.Json;
using Microsoft.IdentityModel.Json.Linq;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.TestUtils;
using Microsoft.IdentityModel.Tokens;
using Xunit;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant

namespace Microsoft.IdentityModel.JsonWebTokens.Tests
{
    public class JsonClaimSetTests
    {
        private static DateTime dateTime = new DateTime(2000, 01, 01, 0, 0, 0);
        private string jsonString = $@"{{""intarray"":[1,2,3], ""array"":[1,""2"",3], ""jobject"": {{""string1"":""string1value"",""string2"":""string2value""}},""string"":""bob"", ""float"":42.0, ""integer"":42, ""nill"": null, ""bool"" : true, ""dateTime"": ""{dateTime}"", ""dateTimeIso8061"": ""{dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)}"" }}";
        private List<Claim> payloadClaims = new List<Claim>()
        {
            new Claim("intarray", @"[1,2,3]", JsonClaimValueTypes.JsonArray, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("array", @"[1,""2"",3]", JsonClaimValueTypes.JsonArray, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("jobject", @"{""string1"":""string1value"",""string2"":""string2value""}", JsonClaimValueTypes.Json, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("string", "bob", ClaimValueTypes.String, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("float", "42.0", ClaimValueTypes.Double, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("integer", "42", ClaimValueTypes.Integer, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("nill", "", JsonClaimValueTypes.JsonNull, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("bool", "true", ClaimValueTypes.Boolean, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("dateTime", dateTime.ToString(), ClaimValueTypes.String, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
            new Claim("dateTimeIso8061", dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ClaimValueTypes.DateTime, "LOCAL AUTHORITY", "LOCAL AUTHORITY"),
        };

        [Theory, MemberData(nameof(ClaimSetTestCases))]
        public void ClaimSetGetValueTests(JsonClaimSetTheoryData theoryData)
        {
            CompareContext context = TestUtilities.WriteHeader($"{this}.ClaimSetTests", theoryData);
            context.IgnoreType = false;
            
            try
            {
                JsonClaimSet jsonClaimSet = new JsonClaimSet(theoryData.Json);
                var retval = typeof(JsonClaimSet).GetMethod("GetValue").MakeGenericMethod(theoryData.PropertyType).Invoke(jsonClaimSet, new object[] { theoryData.PropertyName });
                theoryData.ExpectedException.ProcessNoException(context);
                IdentityComparer.AreEqual(retval, theoryData.PropertyValue, context);
            }
            catch (Exception ex)
            {
                theoryData.ExpectedException.ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        public static TheoryData<JsonClaimSetTheoryData> ClaimSetTestCases()
        {
            var theoryData = new TheoryData<JsonClaimSetTheoryData>();

            #region bool
            theoryData.Add(new JsonClaimSetTheoryData("true")
            {
                Json = $@"{{""true"":true}}",
                PropertyName = "true",
                PropertyType = typeof(bool),
                PropertyValue = true,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("false")
            {
                Json = $@"{{""false"":false}}",
                PropertyName = "false",
                PropertyType = typeof(bool),
                PropertyValue = false,
                ShouldFind = true
            });
            #endregion

            #region datetime
            DateTime dateTime = new DateTime(2000, 01, 01, 0, 0, 0);
            theoryData.Add(new JsonClaimSetTheoryData("datetime")
            {
                Json = $@"{{""datetime"":""{dateTime.ToString()}""}}",
                PropertyName = "datetime",
                PropertyType = typeof(DateTime),
                PropertyValue = dateTime,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("datetimeAsString")
            {
                Json = $@"{{""datetime"":""{dateTime.ToString()}""}}",
                PropertyName = "datetime",
                PropertyType = typeof(string),
                PropertyValue = dateTime.ToString(),
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("dateTimeIso8061")
            {
                Json = $@"{{""dateTimeIso8061"":""{dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)}""}}",
                PropertyName = "dateTimeIso8061",
                PropertyType = typeof(DateTime),
                PropertyValue = dateTime.ToUniversalTime(),
                ShouldFind = true
            });
            #endregion

            #region integer arrays
            theoryData.Add(new JsonClaimSetTheoryData("IntegerAsInt")
            {
                Json = $@"{{""IntegerAsInt"":1}}",
                PropertyName = "IntegerAsInt",
                PropertyType = typeof(int),
                PropertyValue = (int)1,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("IntegerAsIntArray")
            {
                ExpectedException = new ExpectedException(typeof(TargetInvocationException), null, typeof(ArgumentException)) { InnerSubstringExpected = "IDX14305:" },
                Json = $@"{{""IntegerAsIntArray"":1}}",
                PropertyName = "IntegerAsIntArray",
                PropertyType = typeof(int[]),
                PropertyValue = (int) 1,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("IntegerAsObjArray")
            {
                ExpectedException = new ExpectedException( typeof(TargetInvocationException), null , typeof(ArgumentException)){ InnerSubstringExpected = "IDX14305:"},
                Json = $@"{{""IntegerAsObjArray"":1}}",
                PropertyName = "IntegerAsObjArray",
                PropertyType = typeof(object[]),
                PropertyValue = (int)1,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("IntegersAsIntArray")
            {
                Json = $@"{{""IntegersAsIntArray"":[1,2,3]}}",
                PropertyName = "IntegersAsIntArray",
                PropertyType = typeof(int[]),
                PropertyValue = new int[] {1,2,3},
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("IntegersAsObj")
            {
                Json = $@"{{""IntegersAsObj"":[1,2,3]}}",
                PropertyName = "IntegersAsObj",
                PropertyType = typeof(object),
                PropertyValue = new object[]{1, 2, 3},
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("IntegersAsObjArray")
            {
                Json = $@"{{""IntegersAsObjArray"":[1,2,3]}}",
                PropertyName = "IntegersAsObjArray",
                PropertyType = typeof(object[]),
                PropertyValue = new object[] { (long)1, (long)2, (long)3 },
                ShouldFind = true
            });
            #endregion

            #region mixed arrays
            theoryData.Add(new JsonClaimSetTheoryData("MixedArrayAsObjArray")
            {
                Json = $@"{{""MixedArrayAsObjArray"":[1,""2"",3]}}",
                PropertyName = "MixedArrayAsObjArray",
                PropertyType = typeof(object[]),
                PropertyValue = new object[] { 1L, "2", 3L},
                ShouldFind = true
            });
            #endregion

            #region strings
            theoryData.Add(new JsonClaimSetTheoryData("string")
            {
                Json = $@"{{""string"":""bob""}}",
                PropertyName = "string",
                PropertyType = typeof(string),
                PropertyValue = "bob",
                ShouldFind = true
            });
            #endregion

            #region null
            theoryData.Add(new JsonClaimSetTheoryData("nill")
            {
                Json = $@"{{""nill"":null}}",
                PropertyName = "nill",
                PropertyType = typeof(object),
                PropertyValue = null,
                ShouldFind = true
            });
            #endregion

            #region numbers
            theoryData.Add(new JsonClaimSetTheoryData("int")
            {
                Json = $@"{{""int"":42}}",
                PropertyName = "int",
                PropertyType = typeof(int),
                PropertyValue = 42,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("float")
            {
                Json = $@"{{""float"":42.0}}",
                PropertyName = "float",
                PropertyType = typeof(float),
                PropertyValue = (float)42.0,
                ShouldFind = true
            });

            theoryData.Add(new JsonClaimSetTheoryData("double")
            {
                Json = $@"{{""double"":42.0}}",
                PropertyName = "double",
                PropertyType = typeof(double),
                PropertyValue = 42.0,
                ShouldFind = true
            });
            #endregion

            return theoryData;
        }

        // Test checks to make sure that claim values of various types can be successfully retrieved from the header.
        [Fact]
        public void GetValues()
        {
            var context = new CompareContext();
            IdentityModelEventSource.ShowPII = true;
            TestUtilities.WriteHeader($"{this}.GetHeaderValues");

            var token = new JsonWebToken(jsonString, "{}");

            object dateTimeString = token.GetHeaderValue<object>("dateTime");
            try
            {
                DateTime dateTime = token.GetHeaderValue<DateTime>("dateTime");
            }
            catch
            {

            }


            //$@"{{""intarray"":[1,2,3], ""array"":[1,""2"",3], ""jobject"": {{""string1"":""string1value"",""string2"":""string2value""}},""string"":""bob"", ""float"":42.0, ""integer"":42,  ""bool"" : true, ""dateTime"": ""{dateTime}"", ""dateTimeIso8061"": ""{dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)}"" }}";
            // ""intarray"":[1,2,3]
            var intarray = token.GetHeaderValue<int[]>("intarray");
            //IdentityComparer.AreEqual(new int[] { 1, 2, 3 }, intarray, context);

            var intarrayObj = token.GetHeaderValue<object>("intarray");
            //IdentityComparer.AreEqual(new int[] { 1, 2, 3 }, intarrayObj, context);

            var intarrayObjArray = token.GetHeaderValue<object[]>("intarray");
            //IdentityComparer.AreEqual(new int[] { 1, 2, 3 }, intarrayObj, context);

            // ""array"":[1,""2"",3]
            try
            {
                var arrayInt = token.GetHeaderValue<int[]>("array");
                IdentityComparer.AreEqual(new object[] { 1L, "2", 3L }, arrayInt, context);
            }
            catch
            {

            }

            // ""array"":[1,""2"",3]
            var arrayObj = token.GetHeaderValue<object[]>("array");
            //IdentityComparer.AreEqual(new object[] { 1L, "2", 3L }, arrayObj, context);

            //""nill"": null,
            var dateTimeNill = token.GetHeaderValue<DateTime>("nill");
            //IdentityComparer.AreEqual(dateTimeNill, default(DateTime), context);

            var dateTimeNillArray = token.GetHeaderValue<DateTime[]>("nill");
            //IdentityComparer.AreEqual(dateTimeNillArray, default(DateTime[]), context);

            var nillNullObject = token.GetHeaderValue<object>("nill");
            //IdentityComparer.AreEqual(nillNullObject, null, context);

            var nillNullObjectArray = token.GetHeaderValue<object[]>("nill");
            //IdentityComparer.AreEqual(nillNullObjectArray, null, context);

            // only possible internally within the library since we're using Microsoft.IdentityModel.Json.Linq.JObject
            var jobject = token.GetHeaderValue<JObject>("jobject");
            IdentityComparer.AreEqual(JObject.Parse(@"{ ""string1"":""string1value"", ""string2"":""string2value"" }"), jobject, context);

            var name = token.GetHeaderValue<string>("string");
            IdentityComparer.AreEqual("bob", name, context);

            var floatingPoint = token.GetHeaderValue<float>("float");
            IdentityComparer.AreEqual(42.0, floatingPoint, context);

            var integer = token.GetHeaderValue<int>("integer");
            IdentityComparer.AreEqual(42, integer, context);

            var boolean = token.GetHeaderValue<bool>("bool");
            IdentityComparer.AreEqual(boolean, true, context);

            try // Try to retrieve a value that doesn't exist in the header.
            {
                token.GetHeaderValue<int>("doesnotexist");
            }
            catch (Exception ex)
            {
                ExpectedException.ArgumentException("IDX14304:").ProcessException(ex, context);
            }

            try // Try to retrieve an integer when the value is actually a string.
            {
                token.GetHeaderValue<int>("string");
            }
            catch (Exception ex)
            {
                ExpectedException.ArgumentException("IDX14305:", typeof(System.FormatException)).ProcessException(ex, context);
            }

            TestUtilities.AssertFailIfErrors(context);
        }

        // Test checks to make sure that claim values of various types can be successfully retrieved from the payload.
        [Fact]
        public void TryGetPayloadValues()
        {
            var context = new CompareContext();
            TestUtilities.WriteHeader($"{this}.TryGetPayloadValues");

            var token = new JsonWebToken("{}", jsonString);

            var success = token.TryGetPayloadValue("intarray", out int[] intarray);
            IdentityComparer.AreEqual(new int[] { 1, 2, 3 }, intarray, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("array", out object[] array);
            IdentityComparer.AreEqual(new object[] { 1L, "2", 3L }, array, context);
            IdentityComparer.AreEqual(true, success, context);

            // only possible internally within the library since we're using Microsoft.IdentityModel.Json.Linq.JObject
            success = token.TryGetPayloadValue("jobject", out JObject jobject);
            IdentityComparer.AreEqual(JObject.Parse(@"{ ""string1"":""string1value"", ""string2"":""string2value"" }"), jobject, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("string", out string name);
            IdentityComparer.AreEqual("bob", name, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("float", out float floatingPoint);
            IdentityComparer.AreEqual(42.0, floatingPoint, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("integer", out int integer);
            IdentityComparer.AreEqual(42, integer, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("nill", out object nill);
            IdentityComparer.AreEqual(nill, null, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("bool", out bool boolean);
            IdentityComparer.AreEqual(boolean, true, context);
            IdentityComparer.AreEqual(true, success, context);

            var dateTimeValue = token.GetPayloadValue<string>("dateTime");
            IdentityComparer.AreEqual(dateTimeValue, dateTime.ToString(), context);
            IdentityComparer.AreEqual(true, success, context);

            var dateTimeIso8061Value = token.GetPayloadValue<DateTime>("dateTimeIso8061");
            IdentityComparer.AreEqual(dateTimeIso8061Value, dateTime, context);
            IdentityComparer.AreEqual(true, success, context);

            success = token.TryGetPayloadValue("doesnotexist", out int doesNotExist);
            IdentityComparer.AreEqual(0, doesNotExist, context);
            IdentityComparer.AreEqual(false, success, context);

            success = token.TryGetPayloadValue("string", out int cannotConvert);
            IdentityComparer.AreEqual(0, cannotConvert, context);
            IdentityComparer.AreEqual(false, success, context);

            TestUtilities.AssertFailIfErrors(context);
        }

        public class JsonClaimSetTheoryData : TheoryDataBase
        {
            public JsonClaimSetTheoryData(string id) : base(id) { }

            public string Json { get; set; }

            public string PropertyName { get; set; }

            public Type PropertyOut { get; set; }

             public Type PropertyType { get; set; }

            public object PropertyValue { get; set; }

            public bool ShouldFind { get; set; }

            public byte[] Utf8Bytes { get; set; }
        }
    }
}
