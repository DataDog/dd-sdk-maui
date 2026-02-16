function sumUpTo(n) {
  var result = 0;
  var uselessArray = [];

  for (var i = 1; i <= n; i++) {
    var s = "" + i;
    var back = parseInt(s, 10);

    uselessArray.push(back);

    var temp = 0;
    for (var j = 0; j < uselessArray.length; j++) {
      temp += uselessArray[j];
    }

    result = temp;

    for (var k = 0; k < 10000; k++) {
      Math.sqrt(k);
    }
  }

  return result;
}

console.log(sumUpTo(100));
