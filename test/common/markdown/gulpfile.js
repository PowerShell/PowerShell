// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

function runTest() {
    "use strict";
    var gulp = require("gulp");
    var concat = require("gulp-concat");
    var through2 = require("through2");
    var markdownlint = require("markdownlint");

    gulp.task("test-mdsyntax", function task() {
        var paths = [];
        var rootpath;

        // assign --repoRoot <rootpath> into rootpath
        var j = process.argv.indexOf("--rootpath");
        if (j > -1) {
            rootpath = process.argv[j + 1];
        }

        if (rootpath === null) {
            throw "--rootpath <repoRoot> must be specified before all other parameters";
        }

        // parse --filter into paths.  --rootpath must be specified first.
        j = process.argv.indexOf("--filter");
        if (j > -1) {
            var filters = process.argv[j + 1].split(",");
            filters.forEach(function(filter) {
                paths.push(rootpath + "/" + filter);
            }, this);
        }

        if (paths.length === 0) {
            throw "--filter <filter relative to repoRoot> must be specified";
        }

        var rootJsonFile = rootpath + "/.markdownlint.json";
        var fs = require("fs");
        fs.appendFileSync("markdownissues.txt", "--EMPTY--\r\n");
        return gulp.src(paths, { "read": false })
            .pipe(through2.obj(function obj(file, enc, next) {
                markdownlint({
                        "files": [file.path],
                        "config": require(rootJsonFile)
                    },
                    function callback(err, result) {
                        var resultString = (result || "").toString();
                        if (resultString) {
                            file.contents = Buffer.from(resultString);
                        }
                        next(err, file);
                    });
            }))
            .pipe(concat("markdownissues.txt", { newLine: "\r\n" }))
            .pipe(gulp.dest("."));
    });
}

runTest();
