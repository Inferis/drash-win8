package org.inferis.drash;

import android.os.Bundle;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.view.Menu;
import android.view.MotionEvent;
import android.view.View;
import android.widget.RelativeLayout;
import com.nineoldandroids.animation.*;
import com.nineoldandroids.animation.Animator.AnimatorListener;

public class MainActivity extends Activity implements View.OnTouchListener {
	private boolean intensityValueShown;
	
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        
        intensityValueShown = false;

        // disable hardware rendering for ICS so that the actual dotted line is shown
        disableHardwareRendering(findViewById(R.id.dotted_top));
        disableHardwareRendering(findViewById(R.id.dotted_bottom));
        
        // add touch events
        RelativeLayout intensityLayout = (RelativeLayout)findViewById(R.id.intensityLayout);
        intensityLayout.setOnTouchListener(this);
    }

    public boolean onTouch(View v, MotionEvent event) {
    	if (event.getAction() != MotionEvent.ACTION_UP) return true;
    	
    	final View intensityImage = findViewById(R.id.intensityImage);
    	final View intensityValue = findViewById(R.id.intensityValue);
    	
    	if (intensityValueShown) {
    		fadeOut(intensityValue, true, new Completion() {
				public void onComplete() {
		    		fadeIn(intensityImage, true, null);
				}
			});
    	}
    	else {
    		fadeOut(intensityImage, true, new Completion() {
				public void onComplete() {
		    		fadeIn(intensityValue, true, null);
				}
			});
    	}

    	intensityValueShown = !intensityValueShown;
    	return true;
    }
    
    private void fadeIn(final View view, boolean fast, final Completion completion) {
    	AnimatorSet set = new AnimatorSet();
    	set.playTogether(
		    ObjectAnimator.ofFloat(view, "scaleX", 0.9f, 1f),
		    ObjectAnimator.ofFloat(view, "scaleY", 0.9f, 1f),
		    ObjectAnimator.ofFloat(view, "alpha", 0f, 1f)
		);
		set.addListener(new AnimatorListener() {
			public void onAnimationStart(Animator animation) {}
			public void onAnimationRepeat(Animator animation) {}
			public void onAnimationEnd(Animator animation) {
				if (completion != null)	completion.onComplete();
			}
			public void onAnimationCancel(Animator animation) {
				if (completion != null)	completion.onComplete();
			}
		});
		set.setDuration(fast ? 150 : 300).start();
    }
    
    private void fadeOut(final View view, boolean fast, final Completion completion) {
    	AnimatorSet set = new AnimatorSet();
    	set.playTogether(
		    ObjectAnimator.ofFloat(view, "scaleX", 1f, 0.9f),
		    ObjectAnimator.ofFloat(view, "scaleY", 1f, 0.9f),
		    ObjectAnimator.ofFloat(view, "alpha", 1f, 0f)
		);
		set.addListener(new AnimatorListener() {
			public void onAnimationStart(Animator animation) {}
			public void onAnimationRepeat(Animator animation) {}
			public void onAnimationEnd(Animator animation) {
				if (completion != null)	completion.onComplete();
			}
			public void onAnimationCancel(Animator animation) {
				if (completion != null)	completion.onComplete();
			}
		});
		set.setDuration(fast ? 150 : 300).start();
   }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.activity_main, menu);
        return true;
    }
    
    @SuppressLint("NewApi")
    public void disableHardwareRendering(View v) {
        if(android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.HONEYCOMB) {
            v.setLayerType(View.LAYER_TYPE_SOFTWARE, null);
        } 
   }
}
