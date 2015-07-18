/// <binding Clean='clean' />
'use strict';
var gulp = require("gulp"),
  preen = require('preen');

gulp.task('preen', function(cb) {
  preen.preen({}, cb);
});