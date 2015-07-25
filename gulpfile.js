/// <binding Clean='clean' />
'use strict';
var gulp = require("gulp");
var less = require('gulp-less');
var path = require('path');
var concat = require("gulp-concat");
var cssmin = require("gulp-cssmin");
var rimraf = require("rimraf");
var project = require("./project.json");

var paths = {
  webroot: "./" + project.webroot + "/"
};

paths.css = paths.webroot + "css/**/*.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatCssDest = paths.webroot + "css/hawk.min.css";
 
gulp.task('clean:css', function(cb) {
    rimraf(paths.css, cb);
});

gulp.task('less', function () {
  return gulp.src('.\\wwwroot\\css\\*.less', { base: "." })
    .pipe(less())
    .pipe(gulp.dest('.'));    
});

gulp.task('css', ["clean:css", "less"], function() {
    gulp.src([paths.css, "!" + paths.minCss])
      .pipe(concat(paths.concatCssDest))
      .pipe(cssmin())
      .pipe(gulp.dest("."));
})