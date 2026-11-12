#import <Foundation/Foundation.h>
#import <objc/message.h>
#import <dlfcn.h>

// The framework is loaded at runtime, so the Xcode project needs no extra link settings
// (works the same in Unity exports, Codemagic and local builds).
typedef void (*ATTResultCallback)(int status);

static Class ATTManagerClass(void) {
    static Class cls = Nil;
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        dlopen("/System/Library/Frameworks/AppTrackingTransparency.framework/AppTrackingTransparency", RTLD_LAZY);
        cls = NSClassFromString(@"ATTrackingManager");
    });
    return cls;
}

extern "C" {
    // 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized.
    int _ATTStatus(void) {
        Class manager = ATTManagerClass();
        if (!manager) return 3;
        NSUInteger status = ((NSUInteger (*)(Class, SEL))objc_msgSend)(manager, NSSelectorFromString(@"trackingAuthorizationStatus"));
        return (int)status;
    }

    void _ATTRequest(ATTResultCallback callback) {
        Class manager = ATTManagerClass();
        if (!manager) { if (callback) callback(3); return; }
        void (^block)(NSUInteger) = ^(NSUInteger status) {
            dispatch_async(dispatch_get_main_queue(), ^{ if (callback) callback((int)status); });
        };
        id handler = [block copy];
        ((void (*)(Class, SEL, id))objc_msgSend)(manager, NSSelectorFromString(@"requestTrackingAuthorizationWithCompletionHandler:"), handler);
    }
}
