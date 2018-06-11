// <copyright file="TranscriptionDefinition.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace CrisClient
{
    using System;
    using System.Collections.Generic;

    public sealed class TranscriptionDefinition
    {
        private TranscriptionDefinition(string locale, string subscriptionKey, Uri recordingsUrl)
        {
            this.SubscriptionKey = subscriptionKey;
            this.RecordingsUrl = recordingsUrl;
            this.Locale = locale;
        }

        private TranscriptionDefinition(string locale, string subscriptionKey, Uri recordingsUrl, IEnumerable<ModelIdentity> models)
        {
            this.SubscriptionKey = subscriptionKey;
            this.RecordingsUrl = recordingsUrl;
            this.Locale = locale;
            this.Models = models;
        }

        /// <inheritdoc />
        public string SubscriptionKey { get; set; }

        /// <inheritdoc />
        public Uri RecordingsUrl { get; set; }

        public string Locale { get; set; }

        public IEnumerable<ModelIdentity> Models { get; set; }

        public static TranscriptionDefinition Create(
            string locale,
            string subscriptionKey,
            Uri recordingsUrl)
        {
            return new TranscriptionDefinition(locale, subscriptionKey, recordingsUrl, null);
        }

        public static TranscriptionDefinition Create(
            string locale,
            string subscriptionKey,
            Uri recordingsUrl,
            IEnumerable<ModelIdentity> models)
        {
            return new TranscriptionDefinition(locale, subscriptionKey, recordingsUrl, models);
        }
    }
}
