/*
Just a basic c file.
*/

int Add2(int a, int b) {
    return a + b;
}

int Add3(int a, int b, int c) {
    return a + b + c;
}

int AddAll(int[] numbers, int count) {

    int sum = 0;
    for (int i = 0; i < count; i++) {
        sum += numbers[i];
    }

    return sum;
}

// This pragma section may cause problems when trying to find the Multiply scope.
#pragma some_macro(Multiply)
#pragma some_macro(Divide)

int Multiply(int a, int b) {
    return a * b;
}

int Divide(int a, int b) {

    // This should cause a test failure right now since the code that tries to find
    // matching curly braces to determine scope doesn't know how to deal with #if/#else.
#if DIVIDE_BY_ZERO_OK
    if (b >= 0) {
#else
    if (b >= 1) {
#endif
        return a / b;
    }

    return 0;
}

bool Foo(int a, int b, int c) {

    // This function call within an if statement may impact detection of Add2's function definition.
    if (Add2(a, b)) {
        return a + b;
    }
    else {
        return Add2(b, c);
    }
}

int __multiplyImpl(int a, int b) {
    return a * b;
}

bool IsThingEnabled() {
    return GetSetting("MySettings.ThingEnabled");
}
