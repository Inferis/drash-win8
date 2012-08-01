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

@interface ViewController () <CLLocationManagerDelegate>

@property (nonatomic, strong) IBOutlet DataView* dataView;
@property (nonatomic, strong) IBOutlet ErrorView* errorView;
@property (nonatomic, strong) IBOutlet UIActivityIndicatorView* smallSpinner;
@property (nonatomic, strong) IBOutlet UIButton* infoButton;

@end

@implementation ViewController {
    CLLocationManager* _locationManager;
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
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    _rain = nil;
    _locationName = @"";
    _error = nil;
    _reachable = [Drache.network isReachable];
    
    self.errorView.alpha = 0;
    self.dataView.alpha = 0;
    self.smallSpinner.alpha = 0;
    
    CGRect bottomRect = CGRectOffsetTopAndShrink(self.view.bounds, self.view.bounds.size.height-40);
    self.smallSpinner.frame = CGRectCenterIn(self.smallSpinner.frame, bottomRect);
    self.infoButton.frame = CGRectCenterIn(self.infoButton.frame, bottomRect);
  
    UILongPressGestureRecognizer* longTapper = [[UILongPressGestureRecognizer alloc] initWithTarget:self action:@selector(forcedRefresh:)];
    longTapper.minimumPressDuration = 1.5;
    [self.view addGestureRecognizer:longTapper];

    _locationManager = [CLLocationManager new];
    _locationManager.delegate = self;
    _locationManager.distanceFilter = 500;
    _locationManager.desiredAccuracy = kCLLocationAccuracyHundredMeters;
    [_locationManager startUpdatingLocation];
    _location = _locationManager.location;
    
    _firstFetch = _location != nil;
    _geocoder = [CLGeocoder new];
    
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(reachabilityChanged:) name:kReachabilityChangedNotification object:nil];
    
    UIImageView* splash = [[UIImageView alloc] initWithImage:[UIImage imageNamed:@"Default.png"]];
    splash.frame = (CGRect) { 0, -[UIApplication sharedApplication].statusBarFrame.size.height, splash.frame.size };
    splash.tag = 998811;
    [self.view addSubview:splash];
}

- (void)viewWillAppear:(BOOL)animated {
    [super viewWillAppear:animated];
    
    [[UIApplication sharedApplication] setStatusBarStyle:UIStatusBarStyleBlackOpaque animated:YES];
}

- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
    
    UIImageView* splash = (UIImageView*)[self.view viewWithTag:998811];
    if (splash) {
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
    
    if (UIInterfaceOrientationIsLandscape(toInterfaceOrientation) == UIInterfaceOrientationIsLandscape(self.interfaceOrientation))
        return;
    
    if (UIInterfaceOrientationIsLandscape(toInterfaceOrientation)) {
        self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, 0, 5);
        self.infoButton.frame = CGRectOffset(self.infoButton.frame, 0, 5);
    }
    else {
        self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, 0, -5);
        self.infoButton.frame = CGRectOffset(self.infoButton.frame, 0, -5);
    }
}

- (void)willAnimateRotationToInterfaceOrientation:(UIInterfaceOrientation)toInterfaceOrientation duration:(NSTimeInterval)duration {
    [super willAnimateRotationToInterfaceOrientation:toInterfaceOrientation duration:duration];
    
}

- (BOOL)shouldAutorotateToInterfaceOrientation:(UIInterfaceOrientation)interfaceOrientation
{
    return (interfaceOrientation != UIInterfaceOrientationPortraitUpsideDown);
}

- (void)forcedRefresh:(UILongPressGestureRecognizer*)longTapper {
    if (longTapper.state == UIGestureRecognizerStateBegan)
        [self fetchRain];
}

- (IBAction)infoTapped:(id)sender {
    InfoViewController* infoViewController = [[InfoViewController alloc] initWithNibName:nil bundle:nil];
    infoViewController.modalTransitionStyle = UIModalTransitionStyleFlipHorizontal;
    [self presentViewController:infoViewController animated:YES completion:nil];
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
    NSLog(@"delay = %d", delay);
    
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
        NSLog(@"%@", query);
        Tin* tin = [Tin new];
        [tin setTimeoutSeconds:30];
        [tin get:@"http://gps.buienradar.nl/getrr.php" query:query success:^(TinResponse *response) {
            [self endOperation];
            
            RainData* result = nil;
            if (!response.error)
                result = [RainData rainDataFromString:response.bodyString];
            
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
        [UIView animateWithDuration:0.15 animations:^{
            self.smallSpinner.transform = CGAffineTransformMakeScale(0.6, 0.6);
            self.infoButton.transform = CGAffineTransformMakeScale(0.6, 0.6);
        } completion:^(BOOL finished) {
            [UIView animateWithDuration:0.15 animations:^{
                self.smallSpinner.transform = CGAffineTransformIdentity;
                self.infoButton.transform = CGAffineTransformIdentity;
                self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, -20, 0);
                self.infoButton.frame = CGRectOffset(self.infoButton.frame, 20, 0);
                [self.smallSpinner startAnimating];
                self.smallSpinner.alpha = 1;
            }];
        }];
    });
}

- (void)endOperation {
    @synchronized(self) {
        if (--_operations > 0)
            return;
        
        _operations = 0;
    }

    dispatch_sync_main(^{
        [UIView animateWithDuration:0.15 animations:^{
            self.smallSpinner.frame = CGRectOffset(self.smallSpinner.frame, 20, 0);
            self.infoButton.frame = CGRectOffset(self.infoButton.frame, -20, 0);
            self.smallSpinner.alpha = 0;
            self.smallSpinner.transform = CGAffineTransformMakeScale(0.6, 0.6);
            self.infoButton.transform = CGAffineTransformMakeScale(0.6, 0.6);
        } completion:^(BOOL finished) {
            [UIView animateWithDuration:0.15 animations:^{
                self.smallSpinner.transform = CGAffineTransformIdentity;
                self.infoButton.transform = CGAffineTransformIdentity;
            } completion:^(BOOL finished) {
                [self.smallSpinner stopAnimating];
            }];
        }];
    });
}

@end
