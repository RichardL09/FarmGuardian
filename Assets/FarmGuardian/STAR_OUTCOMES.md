# Win, loss and star ratings

This supersedes earlier documentation describing immediate defeat after one theft.

A win requires all animals that remain in the level to be secured, every planting
objective complete, positive time remaining, and no animal still being carried by
the thief. At least one animal must survive. A defeat occurs when time expires or
every animal has been permanently stolen.

Animals count as lost only after the thief enters the visible exit. Recovering an
animal before the exit does not reduce stars. Escaped animals remain in the session
list with `AnimalState.Lost`, are detached from the thief and hidden, and cannot be
counted twice or recovered. After a successful escape the thief returns at another
eligible entrance and immediately pursues another secured animal, or patrols
while waiting for a target.

The one-star threshold is `max(2, ceil(originalAnimalCount / 2))`:

| Original animals | 3 stars | 2 stars | 1 star | Defeat |
| --- | --- | --- | --- | --- |
| 3 | 0 lost | 1 lost | 2 lost | 3 lost |
| 4 | 0 lost | 1 lost | 2–3 lost | 4 lost |
| 5 | 0 lost | 1–2 lost | 3–4 lost | 5 lost |
| 6 | 0 lost | 1–2 lost | 3–5 lost | 6 lost |
| 7 | 0 lost | 1–3 lost | 4–6 lost | 7 lost |
| 8 | 0 lost | 1–3 lost | 4–7 lost | 8 lost |

All star awards still require mission completion; unfinished missions time out as
a defeat with zero stars and no reward. The HUD displays the reduced rescue target
and original/lost counts. Results show the run's stars and the saved personal best.
Each level-map node displays three stars below its number, filled according to
the saved best score. Unplayed levels show three dim stars; locked nodes keep
their lock in the upper-right corner. The displayed score refreshes whenever
the level-selection page is rebuilt, including after a win or replay.

`FarmSave.levelStars` stores the highest score for each of the 100 levels. Legacy
version-1 saves retain coins, upgrades and unlocked levels; missing ratings start
at zero instead of inventing past scores. Lower-scoring replays and defeats do not
erase a previous best. Progress reset clears ratings along with the existing save.
Rewards still use rescued animals, completed planting and remaining time, with
the existing coin upgrade. Won runs unlock the next level and award coins once.

Validation: 23/23 selected PlayMode tests passed on 21 October 2026; see
`Logs/FarmGuardian-star-outcomes-tests.xml`. This includes all gameplay tests,
the unchallenged thief reaching exits across the four maps, and UI navigation /
purchase / pause / reset regression. Tested source files matched the main project
by SHA-256. One- and two-star result previews were rendered and visually inspected
in `Logs/FarmGuardian-one-star-win.png` and `Logs/FarmGuardian-two-star-win.png`.
