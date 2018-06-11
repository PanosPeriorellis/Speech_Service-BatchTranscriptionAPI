// <copyright file="Token.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CrisClient
{
    using Newtonsoft.Json;

    /// <summary>
    /// This is an internal class that still needs to be documented.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Performance",
        "CA1812:AvoidUninstantiatedInternalClasses",
        Justification = "Objects of this class are instantiated by the de-serializer.")]
    public class Token
    {
        /// <summary>
        /// Gets or sets the base64 encoded access token.
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the type of the token
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the duration until the access token expires in seconds.
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the base64 encoded refresh token.
        /// </summary>
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the error message, if an error occurred while fetching the token.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }
}