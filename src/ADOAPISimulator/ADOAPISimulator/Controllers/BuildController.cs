// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AdoApiSimulator.Models;

using Microsoft.AspNetCore.Mvc;

namespace AdoApiSimulator.Controllers
{
    [Route("_apis/[controller]")]
    [ApiController]
    public class BuildController : ControllerBase
    {
        private readonly List<Build> builds = new()
        {
                new Build
                {
                    BuildNumber = "1234",
                    Id = 12,
                },
                new Build
                {
                    BuildNumber = "1235",
                    Id = 13,
                    IsDeleted = true,
                },
                new Build
                {
                    BuildNumber = "1236",
                    Id = 14,
                },
                new Build
                {
                    BuildNumber = "1237",
                    Id = 15,
                },
                new Build
                {
                    BuildNumber = "1238",
                    Id = 16,
                    IsDeleted = true,
                },
                new Build
                {
                    BuildNumber = "1239",
                    Id = 17,
                    IsDeleted = true,
                },
                new Build
                {
                    BuildNumber = "1240",
                    Id = 18,
                },
            };

        [HttpGet("builds", Name = "ListBuilds")]
        public IEnumerable<Build> ListBuilds([FromQuery] string? deletedFilter)
        {
            return deletedFilter == "excludeDeleted"
                ? builds.Where(x => x.IsDeleted == false)
                : builds;
        }

        [HttpGet("builds/{buildId}/artifacts", Name = "GetBuildArtifacts")]
#pragma warning disable IDE0060
        public async Task<ActionResult> GetBuildArtifact(int buildId)
#pragma warning restore IDE0060
        {
            string filePath = @".\Artifacts\CodeAnalysisLogs.zip";
            byte[] bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "text/plain", Path.GetFileName(filePath));
        }
    }
}
