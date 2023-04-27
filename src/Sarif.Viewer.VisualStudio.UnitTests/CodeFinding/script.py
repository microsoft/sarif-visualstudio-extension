import re
import os


equalsPatternOne = r"(\s*)Assert\.AreEqual\((\"[^\"]*\"|[^,]*),\s*(\"[^\"]*\"|[^,]*)\);"
# patternOne = r"(\s*)Assert\.AreEqual\(([^,]+),\s*([^,]+)\);"
equalsPatternTwo = r"(\s*)Assert\.AreEqual\((\"[^\"]*\"|[^,]*),\s*(\"[^\"]*\"|[^,]*),\s*(\$?\"[^\"]*\"|[^,]*)\);"

isTruePatternOne = r"(\s*)Assert.IsTrue\((.*)\);"
isFalsePatternOne = r"(\s*)Assert.IsFalse\((.*)\);"
# testStr = '          Assert.AreEqual(expectedScopeMatchDiff, actualResult.ScopeMatchDiff, $"Expected scope match diff of {expectedScopeMatchDiff}, but actually got {actualResult.ScopeMatchDiff}.");'

# allLines = re.match(patternTwo, testStr)
# print(allLines.group(2))
# print('\n\n')
# print(allLines.group(3))
# print('\n\n')
# newStr = f'{allLines.group(2)}.Should().Be({allLines.group(3)})'
# print(newStr)

def isEqualCheck(line):
    matches = re.match(equalsPatternOne, line)
    newLine = None
    if matches:
        whiteSpace = matches.group(1)
        primaryArg = matches.group(2)
        secondaryArg = matches.group(3)
        if primaryArg.startswith('-'):
            primaryArg = f'({primaryArg})'
        elif primaryArg == 'null':
            primaryArg = secondaryArg
            secondaryArg = 'null' 
        newLine = f'{whiteSpace}{primaryArg}.Should().Be({secondaryArg});\n'
    else:
        matches = re.match(equalsPatternTwo, line)
        if matches:
            whiteSpace = matches.group(1)
            primaryArg = matches.group(2)
            secondaryArg = matches.group(3)
            reason = matches.group(4)
            if primaryArg.startswith('-'):
                primaryArg = f'({primaryArg})'
            elif primaryArg == 'null':
                primaryArg = secondaryArg
                secondaryArg = 'null' 
            newLine = f'{whiteSpace}{primaryArg}.Should().Be({secondaryArg}, {reason});\n'
    return newLine

print(os.listdir('UnitTests'))
for fileName in os.listdir('UnitTests'):
    # fileName = 'IgnoredRegionsTests.cs'
    path = os.path.join('.', 'UnitTests', fileName)
    newFile = []
    with open(path, 'r') as f:
        allLines = f.readlines()
        for line in allLines:
            newLine = line
            equalsOutput = isEqualCheck(line)
            if equalsOutput is not None:
                newLine = equalsOutput
            else: 
                matches = re.match(isTruePatternOne, line)
                if matches:
                    newLine = f'{matches.group(1)}{matches.group(2)}.Should().BeTrue();\n'
                else: 
                    matches = re.match(isFalsePatternOne, line)
                    if matches:
                        newLine = f'{matches.group(1)}{matches.group(2)}.Should().BeFalse();\n'
            newFile.append(newLine)

    with open(path, 'w') as f:
        f.writelines(newFile)
    # break