# Inside-Bar-cBot

###### Break Inside Bar fully automated trading cBot written for cTrader.

This is one of my first bots ever written and my approach to the popular *‘Inside Bar Candle Pattern’* strategy. The strategy idea considers inside bar formation as a moment of market indecision or consolidation. This might make it a relatively good entry point. The pattern consists of 2 bars where the second bar is smaller than the high-low range of the previous bar (regardless of its relative position). The signals are said to be mostly accurate on the H4 and Daily timeframes.

Moreover, the logic includes *StopLoss* and *TakeProfit* management. After having enough pips gained, the code will adjust the SL level to guarantee the profit and increase the TP to try to make a higher gain.

![breakinside](https://user-images.githubusercontent.com/88622607/138148015-49f34be7-26d2-41cf-80a3-6f13c8b2ae62.JPG)

Additionally, I used **Least-Squares Moving Average *(LSMA)*** for signal filtering. The indicator is based on a least squares regression line calculated to the last 89 periods (Fibonacci number) which is later projected forwards to the current moment.

After my tests, I find the strategy accurate in around 60-70% of cases *(backtested in a 10-year-old period on a few currency pairs)*. This is mostly due to a *‘false breakouts’*. It happens when the price initially breaks out beyond the *‘mother candle’* and quickly reverses after it (filling the pending order and then hitting the SL).

The project may still use some adjustments to increase the drawdown.
