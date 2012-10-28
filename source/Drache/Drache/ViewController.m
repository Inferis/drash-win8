//
//  ViewController.m
//  Drache
//
//  Created by Tom Adriaenssen on 13/07/12.
//  Copyright (c) 2012 Tom Adriaenssen. All rights reserved.
//

#import "ViewController.h"
#import <CoreLocation/CoreLocation.h>
#import "Tin.h"
#import "TinResponse.h"
#import "Coby.h"
#import "InfoViewController.h"
#import "DataView.h"
#import "ErrorView.h"
#import "UIView+Pop.h"
#import "RainData.h"
#import "NSUserDefaults+Settings.h"
#import "IIViewDeckController.h"
#import "WBNoticeView.h"
#import "TestFlight.h"

@interface ViewController () <CLLocationManagerDelegate, UIGestureRecognizerDelegate>

@property (nonatomic, strong) IBOutlet DataView* dataView;
@property (nonatomic, strong) IBOutlet ErrorView* errorView;
@property (nonatomic, strong) IBOutlet UIActivityIndicatorView* smallSpinner;
@property (nonatomic, strong) IBOutlet UIButton* infoButton;
@property (nonatomic, strong) IBOutlet UIButton* refreshButton;
@property (nonatomic, strong) IBOutlet UIButton* zoomButton;
@property (nonatomic, strong) IBOutlet UILabel* zoomLabel;
@property (nonatomic, strong) IBOutlet UIImageView* zoomInfo;

@end

@implementation ViewController {
    CLLocation* _location;
    CLGeocoder* _geocoder;
    NSTimer* _timer;
    NSTimer* _locationTimer;
    NSTimer* _geolocationTimer;
    int _operations;
    BOOL _fetchingRain;
    BOOL _rainUpdated;
    RainData* _rain;
    NSString* _locationName;
    NSString* _error;
    BOOL _reachable;
    BOOL _firstFetch;
    int _entries;
    UIPopoverController* _infoController;
    CLLocationManager* _locationManager;
    UITapGestureRecognizer* _tapper;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    _entries = [[NSUserDefaults standardUserDefaults] entries];
    
    _rain = nil;
    _locationName = @"";
    _error = nil;
    _reachable = [Drache.network isReachable];
    _tapper = nil;
    
    self.errorView.alpha = 0;
    self.dataView.alpha = 0;
    self.smallSpinner.alpha = 0;
    
    [self.zoomButton setImage:[UIImage imageNamed:[NSString stringWithFormat:@"dial%d.png", _entries*5]] forState:UIControlStateNormal];
    self.zoomLabel.text = [NSString stringWithFormat:@"%dmin", (_entries*5)];
  
    UILongPressGestureRecognizer* longTapper = [[UILongPressGestureRecognizer alloc] initWithTarget:self action:@selector(forcedRefresh:)];
    longTapper.minimumPressDuration = 1.5;
    [self.view addGestureRecognizer:longTapper];
    
    UIPanGestureRecognizer* zoomer = [[UIPanGestureRecognizer alloc] initWithTarget:self action:@selector(zoomed:)];
    [self.view addGestureRecognizer:zoomer];

    _locationManager = SharedLocationManager;
    _locationManager.delegate = self;
    _locationManager.distanceFilter = 500;
    _locationManager.desiredAccuracy = kCLLocationAccuracyHundredMeters;
    [_locationManager startUpdatingLocation];
    _location = _locationManager.location;
    
    _firstFetch = _location != nil;
    _geocoder = [CLGeocoder new];
    
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(reachabilityChanged:) name:kReachabilityChangedNotification object:nil];
    
    UIImageView* splash = [[UIImageView alloc] initWithImage:[UIImage imageNamed:@"Default.png"]];
    splash.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin | UIViewAutoresizingFlexibleBottomMargin;
    splash.tag = 998811;
    [self.view addSubview:splash];
    
    if (IsIPad()) {
        self.zoomInfo.image = [UIImage imageNamed:@"swipe-ipad.png"];
        self.zoomInfo.frame = CGRectOffsetTopAndShrink(self.zoomInfo.frame, -self.zoomInfo.frame.size.height);
    }
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    
    [self setSplashFrame];
    [[UIApplication sharedApplication] setStatusBarStyle:UIStatusBarStyleBlackOpaque animated:YES];
}

- (void)setSplashFrame {
    UIImageView* splash = (UIImageView*)[self.view viewWithTag:998811];
    if (!splash) return;
    
    if (IsIPad()) {
        splash.frame = (CGRect) { (self.view.bounds.size.width - splash.frame.size.width)/2.0, (self.view.bounds.size.height - splash.frame.size.height)/2.0, splash.frame.size };
    }
    else {
        splash.frame = (CGRect) { 0, (self.view.bounds.size.height - [UIApplication sharedApplication].statusBarFrame.size.height - splash.frame.size.height)/2.0, splash.frame.size };
    }
}

- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
    
    UIImageView* splash = (UIImageView*)[self.view viewWithTag:998811];
    if (splash) {
        [self setSplashFrame];

        splash.tag = 0;
        [UIView animateWithDuration:0.3 delay:0 options:UIViewAnimationOptionCurveEaseIn animations:^{
            splash.transform = CGAffineTransformMakeScale(1.5, 1.5);
            splash.alpha = 0;
        } completion:^(BOOL finished) {
            [splash removeFromSuperview];
            if (_location) {
                [self updateLocation:_location];
                [self fetchRain];
            }
            else
                [self updateState];
        }];
    }
}

- (void)willRotateToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration {
    [super willRotateToInterfaceOrientation:toInterfaceOrientation duration:duration];
    
    if (IsIPad())
        return;
    
    if (UIInterfaceOrientationIsLandscape(toInterfaceOrientation) == UIInterfaceOrientationIsLandscape(self.interfaceOrientation))
        return;
    
    if (UIInterfaceOrientationIsLandscape(toInterfaceOrientation)) {
        self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, 0, 5);
        self.refreshButton.frame = CGRectOffset(self.refreshButton.frame, 0, 5);
        self.infoButton.frame = CGRectOffset(self.infoButton.frame, 0, 5);
    }
    else {
        self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, 0, -5);
        self.refreshButton.frame = CGRectOffset(self.refreshButton.frame, 0, -5);
        self.infoButton.frame = CGRectOffset(self.infoButton.frame, 0, -5);
    }
}

- (void)willAnimateRotationToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration {
    [super willAnimateRotationToInterfaceOrientation:toInterfaceOrientation duration:duration];
    
    [self removeTapper:NO];
}

- (BOOL)shouldAutorotate {
    return YES;
}

- (NSUInteger)supportedInterfaceOrientations {
    if (IsIPad()) return UIInterfaceOrientationMaskAll;
    return UIInterfaceOrientationMaskAllButUpsideDown;
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    if (IsIPad()) return YES;
    return (interfaceOrientation != UIInterfaceOrientationPortraitUpsideDown);
}

- (IBAction)zoomTapped:(id)sender {
    NSLog(@"tapper = %@", _tapper);
    if (_tapper) {
        [self removeTapper:NO];
        return;
    }
    
    [self.zoomInfo popInCompletion:^{
        _tapper = [UITapGestureRecognizer new];
        _tapper.numberOfTapsRequired = 1;
        _tapper.numberOfTouchesRequired = 1;
        [self.view addGestureRecognizer:_tapper];
        _tapper.delegate = self;
    }];
}

- (void)removeTapper:(BOOL)delayed {
    [self.view removeGestureRecognizer:_tapper];
    
    dispatch_delayed(0.1, ^{
        _tapper = nil;
    });
    if (delayed) {
        dispatch_delayed(0.5, ^{
            [self.zoomInfo popOutCompletion:nil];
        });
    }
    else
        [self.zoomInfo popOutCompletion:nil];
}

- (BOOL)gestureRecognizer:(UIGestureRecognizer *)gestureRecognizer shouldRecognizeSimultaneouslyWithGestureRecognizer:(UIGestureRecognizer *)otherGestureRecognizer {
    [self removeTapper:[otherGestureRecognizer isKindOfClass:[UIPanGestureRecognizer class]]];
    return YES;
}

- (IBAction)forcedRefresh {
    [self fetchRain];
}

- (void)forcedRefresh:(UILongPressGestureRecognizer*)longTapper {
    if (longTapper.state == UIGestureRecognizerStateBegan)
        [self fetchRain];
}

- (void)zoomed:(UIPanGestureRecognizer*)zoomer {
    if (zoomer.state != UIGestureRecognizerStateChanged)
        return;
    
    CGPoint delta = [zoomer translationInView:self.view];
    if (ABS(delta.y) < 25 && ABS(delta.x) > 30) {
        int factor = 1 + (int)floorf(ABS([zoomer velocityInView:self.view].x)/1000.0);

        int entries = _entries + (delta.x < 0 ? 3 : -3)*factor;
        entries = MIN(MAX(6, entries), 24);
        
        if (_entries != entries) {
            [self.zoomButton setImage:[UIImage imageNamed:[NSString stringWithFormat:@"dial%d.png", entries*5]] forState:UIControlStateNormal];
            self.zoomLabel.text = [NSString stringWithFormat:@"%dmin", (entries*5)];
            
            _entries = entries;
            //NSLog(@"entries = %i", entries);
            [[NSUserDefaults standardUserDefaults] setEntries:entries];
            [self updateState];
            [zoomer setTranslation:CGPointZero inView:self.view];
        }
        else if (_entries == 24 || _entries == 6)
            [zoomer setTranslation:CGPointZero inView:self.view];
    }
}


- (IBAction)infoTapped:(UIButton*)sender {
    InfoViewController* infoViewController = [[InfoViewController alloc] initWithNibName:nil bundle:nil];
    
    if (self.viewDeckController) {
        [self.viewDeckController toggleRightView];
    }
    else {
        infoViewController.modalTransitionStyle = UIModalTransitionStyleFlipHorizontal;
        [self presentViewController:infoViewController animated:YES completion:nil];
    }
}

#pragma mark - network

- (void)reachabilityChanged:(NSNotification*)notification {
    dispatch_delayed(1, ^{
        BOOL shouldFetch = _reachable != [Drache.network isReachable] && [Drache.network isReachable];
        [self updateState];
        if (shouldFetch) [self fetchRain];
    });
}

#pragma mark - Location Manager Delegate

- (void)updateLocation:(CLLocation*)location {
    if (!location) {
        _location = nil;
        _locationName = nil;
        [_geolocationTimer invalidate];
        _geolocationTimer = nil;
        [_locationTimer invalidate];
        _locationTimer = nil;
        [self updateState];
        return;
    }
    
    int delay = (_firstFetch || !_location || [_location distanceFromLocation:location] > 500) ? 0 : 2;
    
    [_locationTimer invalidate];
    _locationTimer = [NSTimer scheduledTimerWithTimeInterval:delay target:self selector:@selector(updateLocation2:) userInfo:location repeats:NO];
}

- (void)updateLocation2:(NSTimer*)timer {
    _locationTimer = nil;
    
    _location = (CLLocation*)timer.userInfo;
    [self updateState];
    
    if (!_location) {
        _locationName = nil;
        [_geolocationTimer invalidate];
        _geolocationTimer = nil;
    }
    else {
        [self fetchRain];
        [_geolocationTimer invalidate];
        _geolocationTimer = [NSTimer scheduledTimerWithTimeInterval:0.3 target:self selector:@selector(lookupLocation) userInfo:nil repeats:NO];
    }
}
    
- (void)lookupLocation {
    if (_geocoder.geocoding) {
        [_geolocationTimer invalidate];
        _geolocationTimer = [NSTimer scheduledTimerWithTimeInterval:1 target:self selector:@selector(lookupLocation) userInfo:nil repeats:NO];
        return;
    }
    
    [self startOperation];
    [_geocoder reverseGeocodeLocation:_location completionHandler:^(NSArray *placemarks, NSError *error) {
        [self endOperation];
        
        if (!_location)
            return;
        
        NSString* location = [NSString stringWithFormat:@"%0.5f,%0.5f", _location.coordinate.latitude, _location.coordinate.longitude];
        if (!IsEmpty(placemarks)) {
            CLPlacemark* mark = [placemarks first];
            if (!IsEmpty(mark.locality)) {
                if (!IsEmpty(mark.country)) {
                    location = [NSString stringWithFormat:@"%@, %@", mark.locality, mark.country];
                }
                else
                    location = mark.locality;
            }
        }
        
        _locationName = location;
        _geolocationTimer = nil;
        [self updateState];
    }];
}

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {
    if (status != kCLAuthorizationStatusDenied)
        [manager startUpdatingLocation];
    else
        [manager stopUpdatingLocation];

    if (_firstFetch) return;
    [self updateLocation:manager.location];
}

- (void)locationManager:(CLLocationManager *)manager didUpdateToLocation:(CLLocation *)newLocation fromLocation:(CLLocation *)oldLocation {
    if (_firstFetch) return;

    if (_location && [_location distanceFromLocation:newLocation] < 20)
        return;
    
    [self updateLocation:newLocation];
}

- (void)updateState {
    @try {
        if (![Drache.network isReachable]) {
            _error = @"nonetwork";
            _reachable = NO;
            return;
        }
        _reachable = YES;
        
        if (!_location) {
            _error = @"nolocation";
            return;
        }

        _error = nil;
    }
    @finally {
        [self updateVisuals];
    }
}

- (void)updateVisuals {
    if (_error) {
        [self visualizeError:_error];
        return;
    }
    else if (self.errorView.alpha > 0) {
        [UIView animateWithDuration:0.3 animations:^{
            self.errorView.alpha = 0;
        } completion:^(BOOL finished) {
            [self updateVisuals];
        }];
        return;
    }
    
    if (IsEmpty(_locationName) && !_rain) {
        // nothing to see
        if (self.dataView.alpha > 0)
            [self.dataView popOutCompletion:nil];
        return;
    }

    // location and/or chance visible

    // first case: data view invisible, just set the data and show it
    [self visualizeRain:_rain];
    [self visualizeLocation:_locationName];
}

- (void)visualizeError:(NSString*)error {
    if (self.dataView.alpha != 0) {
        // data view displayed: hide it first
        [self.dataView popOutCompletion:^{
            [self visualizeError:error];
        }];
        return;
    }

    // data view hidden. Now: error view hidden? --> YES
    // just fade it in.
    if (self.errorView.alpha == 0) {
        [self.errorView setError:error animated:NO];
        [self.errorView popInCompletion:nil];
        return;
    }

    // error view displayed. if error is different, set it
    [self.errorView setError:error animated:YES];
}


- (void)showData:(void(^)(BOOL animated))action {
    if (self.errorView.alpha != 0) {
        // error view displayed: hide it first
        [self.errorView popOutCompletion:^{
            [self showData:action];
        }];
        return;
    }
    
    // error view hidden. Now: data view hidden? --> YES
    // just fade it in.
    if (self.dataView.alpha == 0) {
        if (action) action(NO);
        [UIView animateWithDuration:0.3 animations:^{
            self.dataView.alpha = 1;
        }];
//        [self.dataView popInCompletion:nil];
        return;
    }
    
    if (action) action(YES);
}

- (void)visualizeLocation:(NSString*)location {
    [self showData:^(BOOL animated) {
        [self.dataView setLocation:location animated:animated];
    }];
}

- (void)visualizeRain:(RainData*)rain {
    BOOL updateAnimated = _rainUpdated;
    _rainUpdated = NO;
    [self showData:^(BOOL animated) {
        [self.dataView setRain:rain
                      animated:updateAnimated && animated];
    }];
}

- (void)fetchRain {
    [_timer invalidate];
    _timer = nil;
    
    if (_error || _fetchingRain)
        return;
    
    _firstFetch = NO;
    _fetchingRain = YES;
    [self startOperation];
    dispatch_async_bg(^{
        [NSThread sleepForTimeInterval:2]; // to avoid flickering :)
        
        NSString* query = [NSString stringWithFormat:@"lat=%f&lon=%f",
                           _locationManager.location.coordinate.latitude,
                           _locationManager.location.coordinate.longitude];
        Tin* tin = [Tin new];
        [tin setTimeoutSeconds:30];
        [tin get:@"http://gps.buienradar.nl/getrr.php" query:query success:^(TinResponse *response) {
            [self endOperation];
            
            RainData* result = nil;
            if (!response.error)
                result = [RainData rainDataFromString:response.bodyString];
            else {
                [[WBNoticeView defaultManager] showErrorNoticeInView:self.view
                                                               title:@"Network issue"
                                                             message:@"Could not fetch rain prediction data at this moment."];
                TFLog(@"fetchrain error: %@", response.error);
            }
            
            _fetchingRain = NO;
            _timer = [NSTimer scheduledTimerWithTimeInterval:3*60 target:self selector:@selector(fetchRain) userInfo:nil repeats:NO];
            _rain = result;
            _rainUpdated = YES;
            [self updateState];
        }];
    });
}

- (void)startOperation {
    @synchronized(self) {
        if (_operations++)
            return;
    }
    
    dispatch_sync_main(^{
        [self.refreshButton popOutCompletion:^{
            [self.smallSpinner startAnimating];
            [self.smallSpinner popInCompletion:nil fast:YES];
        } fast:YES];
    });
}

- (void)endOperation {
    @synchronized(self) {
        if (--_operations > 0)
            return;
        
        _operations = 0;
    }

    dispatch_sync_main(^{
        [self.smallSpinner popOutCompletion:^{
            [self.smallSpinner stopAnimating];
            [self.refreshButton popInCompletion:nil fast:YES];
        } fast:YES];
    });
}

@end
