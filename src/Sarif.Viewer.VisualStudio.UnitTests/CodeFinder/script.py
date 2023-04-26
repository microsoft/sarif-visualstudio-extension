import re

pattern = r"Assert.AreEqual\(([A-z,0-9,\[,\]]*), ([A-z,0-9,\[,\],.]*)\)"

testStr = 'Assert.AreEqual(10, results[0].LineNumber)'

x = re.match(pattern, testStr)
print(x.group(1))
print(x.group(2))
newStr = f'{x.group(1)}.Should().Be({x.group(2)})'
print(newStr)

