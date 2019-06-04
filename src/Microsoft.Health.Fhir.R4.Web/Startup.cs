﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Health.Fhir.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddDevelopmentIdentityProvider(Configuration);

            Core.Registration.IFhirServerBuilder fhirServerBuilder = services.AddFhirServer(Configuration)
                .AddExportWorker()
                .AddKeyVaultSecretStore(Configuration);

            string dataStore = Configuration["DataStore"];
            if (dataStore.Equals(KnownDataStores.CosmosDb, StringComparison.InvariantCultureIgnoreCase))
            {
                fhirServerBuilder.AddCosmosDb(Configuration);
            }
            else if (dataStore.Equals(KnownDataStores.SqlServer, StringComparison.InvariantCultureIgnoreCase))
            {
                ////fhirServerBuilder.AddExperimentalSqlServer();
            }

            AddApplicationInsightsTelemetry(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app)
        {
            IEnumerable<ITestMiddleware> testMiddleware = app.ApplicationServices.GetServices<ITestMiddleware>();

            if (testMiddleware != null)
            {
                foreach (ITestMiddleware middleware in testMiddleware)
                {
                    app.UseMiddleware(middleware.GetType());
                }
            }

            app.UseFhirServer();

            app.UseDevelopmentIdentityProvider();
        }

        /// <summary>
        /// Adds ApplicationInsights for telemetry and logging.
        /// </summary>
        private void AddApplicationInsightsTelemetry(IServiceCollection services)
        {
            string instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];

            if (!string.IsNullOrWhiteSpace(instrumentationKey))
            {
                services.AddApplicationInsightsTelemetry(instrumentationKey);
                services.AddLogging(loggingBuilder => loggingBuilder.AddApplicationInsights(instrumentationKey));
            }
        }
    }
}