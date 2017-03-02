using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.math;
namespace CSGO_Analytics.src.encounterdetect.utils
{
    class BresenhamGridCollision
    {
        public bool performBresenhamLineStepping(EDVector3D actorpos, EDVector3D recieverpos, EDRect[][] grid)
        {           
            var x1 = actorpos.X;
            var y1 = actorpos.Y;
            var x2 = recieverpos.X;
            var y2 = recieverpos.Y;
            var max_distance = 10;
            var check_start = false;
            var check_end = false;

            int i;               // Loop counter
            float ystep, xstep;    // The step on y and x axes
            float error;           // The error accumulated during the increment
            float errorprev;       // Stores the previous value of the error variable
            float yy = y1; float xx = x1;// The line points
            float ddy, ddx;        // Compulsory variables: the double values of dy and dx
            var dx = x2 - x1;
            var dy = y2 - y1;

            // If you want to make the script failsafe, you should see if any coordinate is outside of the grid here

            // Check distance start and end coordinates
            if (max_distance > 0 && EDMathLibrary.getEuclidDistance2D(actorpos, recieverpos) > max_distance)
                return false;

            // Check start and end coordinates directly to save possible execution time 
            if ((check_start && ds_grid_get(grid, x1, y1) > 0) || (check_end && ds_grid_get(grid, x2, y2) > 0))
                return false;

            if (dy < 0)
            {
                ystep = -1;
                dy = -dy;
            }
            else
                ystep = 1;
            if (dx < 0)
            {
                xstep = -1;
                dx = -dx;
            }
            else
                xstep = 1;
            ddy = 2 * dy; // Work with double values for full precision
            ddx = 2 * dx;
            if (ddx >= ddy)
            { // First octant (0 <= slope <= 1)
              // Compulsory initialization (even for errorprev, needed when dx==dy)
                errorprev = dx; // Start in the middle of the square
                error = dx;
                for (i = 0; i < dx; i++)
                { // Do not use the first point (already done)
                    xx += xstep;
                    error += ddy;
                    if (error > ddx)
                    {  // Increment y if AFTER the middle ( > )
                        yy += ystep;
                        error -= ddx;
                        // Three cases (octant == right->right-top for directions below):
                        if (error + errorprev < ddx)
                        { // Bottom square also
                            if (ds_grid_get(grid, xx, yy - ystep) > 0)
                                return false;
                        }
                        else if (error + errorprev > ddx)
                        {  // Left square also
                            if (ds_grid_get(grid, xx - xstep, yy) > 0)
                                return false;
                        }
                        else
                        { // Corner: bottom and left squares also (line goes exactly through corner of cells)
                            if (ds_grid_get(grid, xx, yy - ystep) > 0 && ds_grid_get(grid, xx - xstep, yy) > 0) // If both positions are occupied we return false, the AND can be changed to OR
                                return false;
                        }
                    }
                    if (ds_grid_get(grid, xx, yy) > 0)
                        return false;
                    errorprev = error;
                }
            }
            else
            {  // The same as above
                errorprev = dy;
                error = dy;
                for (i = 0; i < dy; i++)
                {
                    yy += ystep;
                    error += ddx;
                    if (error > ddy)
                    {
                        xx += xstep;
                        error -= ddy;
                        if (error + errorprev < ddy)
                        {
                            if (ds_grid_get(grid, xx - xstep, yy) > 0)
                                return false;
                        }
                        else if (error + errorprev > ddy)
                        {
                            if (ds_grid_get(grid, xx, yy - ystep) > 0)
                                return false;
                        }
                        else
                        {
                            if (ds_grid_get(grid, xx - xstep, yy) > 0 && ds_grid_get(grid, xx, yy - ystep) > 0)
                                return false;
                        }
                    }
                    if (ds_grid_get(grid, xx, yy) > 0)
                        return false;
                    errorprev = error;
                }
            }

            // We have reached the end point so return true, we have line of sight
            return true;
        }

        private int ds_grid_get(EDRect[][] grid, float x1, float y1)
        {
            throw new NotImplementedException();
        }
    }
}
