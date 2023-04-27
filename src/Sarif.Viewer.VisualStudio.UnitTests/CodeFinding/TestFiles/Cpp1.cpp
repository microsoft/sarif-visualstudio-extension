#include <string>

using namespace TextMatcherTest;

bool g_SomeGlobal = false;

Test1::Test1(
    const std::wstring Name
)
{
    this.Name = Name;
}

int
Test1::Add2(
    int a,
    int b)
{
    return a + b;
}

int
Test1::Add3(
    int a,
    int b,
    int c)
{
    return a + b + c;
}

int
Test1::AddMore(
    int[] Numbers,
    int Count
    )
{
    int sum = 0;
    for (int i = 0; i < Count; i++)
    {
        sum += Numbers[i];
    }
    return sum;
}

int
Test1::Sub(
    int a,
    int b)
{
    return a - b;
}

int 
Test1::Sub(
    int a,
    int b,
    int c
)
{
    return a - b - c;
}

// Class definition.
public class CppTest {

private:
    std::wstring Name;

public:
    CppTest(const std::wstring name);
    ~CppTest();
    int Multiply2(int a, int b);
    int Multiply3(int a, int b, int c);
    int Multiply(int[] numbers, int count);

    void Rename(const std::wstring name) {
        this.Name = name;
    }
};

// Class implementation.
CppTest::CppTest(const std::wstring name) {
    this.Name = name;
}

CppTest::~CppTest() {
    free(this.Name);
}

int CppTest::Multiply2(int a, int b) {
    return a * b;
}

int CppTest::Multiply3(int a, int b, int c) {
    return a * b * c;
}

int CppTest::Multiply(int[] numbers, int count) {

    int product = 1;
    try {
        for (int i = 0; i < count; i++) {
            product *= numbers[i];
        }
    }
    catch {
        product = -1;
    }

    return product;
}

class BatchManager {

    template<typename TFunc>
    void EnumerateBatchesAndExecute(TFunc&& func)
    {
        std::vector<std::shared_ptr<ICreativeBatch>> existingBatches = EnumerateBatches();
        for (auto const& batch : existingBatches)
        {
            bool const shouldContinue = func(batch);
            if (!shouldContinue)
            {
                break;
            }
        }
    }
};

namespace System {
    namespace Math {

        int AddImpl(int a, int b) {
            return a + b;
        }

        int Adder::Add(int[] numbers, count) {
            int sum = 0;
            for (int i = 0; i < count; i++) {
                sum += numbers[i];
            }
            return sum;
        }

        HRESULT Divider::Divide(
            int a,
            int b,
            int* quotient
        )
        try
        {
            THROW_IF_FAILED(b > 0);
            *quotient = a / b;
            return S_OK;
        }
        CATCH_RETURN()
    }
}

class TimerBase : public SingletonHelper::Callback
{
public:
    TimerBase(int duration) :
        Callback()
    {
        this.duration = duration;
    }

    virtual void DoStuff() = 0;

    static void TimerCallback(PVOID Context) {
        if (Context) {
            reinterpret_cast<TimerBase>(Context)->DoStuff();
        }
    }

private:
    int duration;
};

struct ThreadDispatcher
{
    HANDLE dispatcherHandle;

    void Initialize(HANDLE handle)
    {
        dispatcherHandle = handle;
    }

    void Cleanup()
    {
        dispatcherHandle = nullptr;
    }
};

class Dispatcher
{
public:
    Dispatcher(std::string name)
    {
        this.name = name;
    }

private:
    struct SensorDispatcher
    {
        HANDLE dispatcherHandle;

        void Initialize(HANDLE handle)
        {
            dispatcherHandle = handle;
        }

        void Cleanup()
        {
            dispatcherHandle = nullptr;
        }
    };

    std::string name = DISPATCHER_NAME_DEFAULT;
};

Dispatcher* DispatcherFactory::CreateDispatcher()
{
    return new Dispatcher();
}

namespace
{
    bool IsEven(int number)
    {
        return (number % 2 == 0);
    }
}

namespace TemplateTest
{
    class Test1 {

        template <typename MyType>
        int Find(MyType[] items, int itemsCount, MyType& toFind)
        {
            for (int i = 0; i < count; i++)
            {
                if (items[i] == toFind)
                {
                    return i;
                }
            }

            return -1;
        }

        template <typename MyType, typename CallbackType>
        void FindAndCallback(MyType[] items, int itemsCount, MyType& toFind, CallbackType& callback)
        {
            for (int i = 0; i < count; i++)
            {
                if (items[i] == toFind)
                {
                    callback(i);
                    return;
                }
            }

            callback(-1);
        }
    };
}