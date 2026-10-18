#!/usr/bin/env python3
"""Small deterministic balance model for the jury build; uses only Python's stdlib."""

from __future__ import annotations

import json
import math
import random
from dataclasses import asdict, dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "docs" / "JuryBalance.json"

DAYS = {"fire": 5, "flood": 10, "quake": 15, "tsunami": 20}
BASE_LOSS = {"fire": 120, "flood": 150, "quake": 200, "tsunami": 250}
REQUIRES = {
    "fire": {"fire": 2},
    "flood": {"infrastructure": 2},
    "quake": {"housing": 2, "school": 2, "akut": 2, "fire": 2},
    "tsunami": {"breakwater": 3},
}
UPGRADE_COSTS = {
    "housing": (200, 400),
    "school": (200, 400),
    "akut": (250, 500),
    "fire": (250, 400),
    "breakwater": (350, 650),
    "infrastructure": (300, 550),
}
REPAIR_COSTS = {
    "housing": 100,
    "school": 100,
    "akut": 125,
    "fire": 125,
    "breakwater": 175,
    "infrastructure": 150,
}
HOUSING_CAPACITY = (1000, 1300, 1600)
HOUSING_MULTIPLIER = (1.0, 1.35, 1.75)
SCHOOL_CAP = (40, 70, 100)


def rounded(value: float) -> int:
    """Match Mathf.RoundToInt for the positive casualty values used here."""
    return math.floor(value + 0.5)


@dataclass
class Policy:
    name: str
    targets: list[tuple[str, int, int]] = field(default_factory=list)
    quiz_days: tuple[int, ...] = ()
    repair_after_events: tuple[str, ...] = ()


@dataclass
class Run:
    policy: str
    cash: int = 500
    population: int = 1000
    awareness: float = 10.0
    levels: dict[str, int] = field(default_factory=lambda: {name: 1 for name in UPGRADE_COSTS})
    damaged: set[str] = field(default_factory=set)
    deaths: int = 0
    defended: int = 0
    answered: int = 0
    quiz_count: int = 0
    purchases: list[str] = field(default_factory=list)
    repairs: list[str] = field(default_factory=list)
    incidents: list[dict] = field(default_factory=list)
    cash_timeline: list[dict] = field(default_factory=list)
    action_failures: list[str] = field(default_factory=list)
    targets_reached: set[tuple[str, int, int]] = field(default_factory=set)
    quiz_days_used: list[int] = field(default_factory=list)

    def upgrade_cost(self, building: str) -> int:
        return UPGRADE_COSTS[building][self.levels[building] - 1]

    def upgrade(self, building: str, target_level: int, day: int) -> bool:
        while self.levels[building] < target_level:
            if building in self.damaged:
                return False
            cost = self.upgrade_cost(building)
            if self.cash < cost:
                return False
            self.cash -= cost
            self.levels[building] += 1
            self.purchases.append(f"day {day}: {building} -> {self.levels[building]} (-{cost})")
        return True

    def repair(self, building: str, day: int) -> bool:
        if building not in self.damaged or self.cash < REPAIR_COSTS[building]:
            return False
        cost = REPAIR_COSTS[building]
        self.cash -= cost
        self.damaged.remove(building)
        self.repairs.append(f"day {day}: {building} (-{cost})")
        return True

    def run_policy(self, policy: Policy) -> dict:
        pending = list(policy.targets)
        for day in range(1, 21):
            # Spend toward the nearest unfinished defense deadline first. Among equal
            # deadlines, preserve the declaration order so the policy is reproducible.
            for building, target_level, deadline in sorted(pending, key=lambda item: item[2]):
                if day > deadline or self.levels[building] >= target_level:
                    continue
                self.upgrade(building, target_level, day)

            for target in policy.targets:
                building, target_level, deadline = target
                if day <= deadline and self.levels[building] >= target_level and building not in self.damaged:
                    self.targets_reached.add(target)

            if day in policy.quiz_days and self.answered < 2 and "school" not in self.damaged:
                cap = SCHOOL_CAP[self.levels["school"] - 1]
                self.awareness = min(cap, self.awareness + 25)
                self.answered += 1
                self.quiz_count += 1
                self.quiz_days_used.append(day)

            incident = next((name for name, event_day in DAYS.items() if event_day == day), None)
            if incident is not None:
                self.resolve_incident(incident, day)
                for building in policy.repair_after_events:
                    self.repair(building, day)

            if day < 20:
                multiplier = HOUSING_MULTIPLIER[self.levels["housing"] - 1]
                income = rounded(self.population * 0.1 * multiplier * (0.5 if "housing" in self.damaged else 1.0))
                self.cash += income
                capacity = HOUSING_CAPACITY[self.levels["housing"] - 1]
                if "housing" not in self.damaged and self.population < capacity:
                    self.population = min(capacity, self.population + 30)
            self.cash_timeline.append({"day": day, "cash": self.cash, "population": self.population})

        return {
            "policy": policy.name,
            "cash": self.cash,
            "population": self.population,
            "awareness": self.awareness,
            "quizDays": self.quiz_days_used,
            "defendedIncidents": self.defended,
            "totalDeaths": self.deaths,
            "levels": dict(self.levels),
            "damagedAtEnd": sorted(self.damaged),
            "purchases": self.purchases,
            "repairs": self.repairs,
            "targetsReached": [f"day {deadline}: {building} reached level {level}"
                               for building, level, deadline in policy.targets
                               if (building, level, deadline) in self.targets_reached],
            "incidents": self.incidents,
            "cashTimeline": self.cash_timeline,
            "failedTargets": [f"day {deadline}: {building} did not reach level {level} by its deadline"
                              for building, level, deadline in policy.targets
                              if (building, level, deadline) not in self.targets_reached],
        }

    def resolve_incident(self, incident: str, day: int) -> None:
        is_defended = incident_defended(incident, self.levels, self.damaged)
        if is_defended:
            self.defended += 1

        newly_damaged = []
        if not is_defended:
            if incident == "fire":
                vulnerable = ("housing", "school")
            elif incident == "flood":
                vulnerable = ("infrastructure", "housing")
            elif incident == "quake":
                vulnerable = tuple(building for building, required in REQUIRES[incident].items()
                                   if self.levels[building] < required or building in self.damaged)
            else:
                vulnerable = ("breakwater", "housing")
            for building in vulnerable:
                self.damage(building, newly_damaged)

        base = BASE_LOSS[incident] * (0.2 if is_defended else 1.0)
        akut_level = 0 if "akut" in self.damaged else self.levels["akut"]
        factor = max(0.0, (1.0 - self.awareness * 0.006) * (1.0 - akut_level * 0.1))
        deaths = min(self.population, rounded(base * factor))
        self.population -= deaths
        self.deaths += deaths

        self.incidents.append({
            "day": day,
            "incident": incident,
            "defended": is_defended,
            "awareness": self.awareness,
            "akutLevelUsed": akut_level,
            "baseLoss": base,
            "deaths": deaths,
            "damaged": newly_damaged,
        })

    def damage(self, building: str, newly_damaged: list[str]) -> None:
        if building in self.damaged:
            return
        self.damaged.add(building)
        newly_damaged.append(building)


def incident_defended(incident: str, levels: dict[str, int], damaged: set[str]) -> bool:
    return all(levels[name] >= required and name not in damaged
               for name, required in REQUIRES[incident].items())


def card_index(defended: int, awareness: float, cash: int, deaths: int) -> int:
    if defended == 4 and awareness >= 60:
        return 3
    if cash >= 900 and deaths >= 240:
        return 0
    if defended >= 3 and awareness < 40:
        return 1
    if defended <= 2 and awareness >= 60 and deaths <= 320:
        return 2
    return 4


def scenarios() -> list[Policy]:
    return [
        Policy("leader: defend all and answer two quizzes", [
            ("fire", 2, 4), ("infrastructure", 2, 9), ("housing", 2, 14),
            ("school", 2, 14), ("akut", 2, 14), ("breakwater", 3, 19),
        ], quiz_days=(4, 6)),
        Policy("rich: save cash without educating or defending"),
        Policy("concrete: defend every event, keep awareness low, spend on resilience", [
            ("fire", 2, 4), ("infrastructure", 2, 9), ("housing", 2, 14),
            ("school", 2, 14), ("akut", 3, 14), ("breakwater", 3, 19),
        ]),
        Policy("camp: learn, keep defenses selective, repair after the flood and quake", [
            ("fire", 2, 4), ("infrastructure", 2, 9), ("school", 2, 14), ("akut", 3, 14),
        ], quiz_days=(4, 6), repair_after_events=("akut", "housing")),
        Policy("fallback: educate and strengthen response, but leave protection incomplete", [
            ("school", 2, 14), ("akut", 3, 14),
        ], quiz_days=(4, 6)),
        Policy("leader plus housing 3 and school 3 after core defenses", [
            ("fire", 2, 4), ("infrastructure", 2, 9), ("housing", 2, 14),
            ("school", 2, 14), ("akut", 2, 14), ("breakwater", 3, 19),
            ("housing", 3, 20), ("school", 3, 20),
        ], quiz_days=(4, 6)),
    ]


def run_random_policy(seed: int) -> Run:
    """Random affordable purchases, capped quizzes, and optional repairs; no riverbed event."""
    rng = random.Random(seed)
    run = Run(f"random spending seed {seed}")
    quiz_count = 0
    for day in range(1, 21):
        daily_purchases = rng.choices((0, 1, 2, 3), weights=(5, 55, 35, 5), k=1)[0]
        for _ in range(daily_purchases):
            affordable = [name for name in UPGRADE_COSTS
                          if run.levels[name] < 3 and name not in run.damaged and run.cash >= run.upgrade_cost(name)]
            if not affordable:
                break
            building = rng.choice(affordable)
            run.upgrade(building, run.levels[building] + 1, day)

        school_cap = SCHOOL_CAP[run.levels["school"] - 1]
        if (quiz_count < 2 and "school" not in run.damaged and run.awareness < school_cap and
                rng.random() < .2):
            run.awareness = min(school_cap, run.awareness + 25)
            quiz_count += 1
            run.quiz_count += 1
            run.quiz_days_used.append(day)

        incident = next((name for name, event_day in DAYS.items() if event_day == day), None)
        if incident is not None:
            run.resolve_incident(incident, day)
            if run.damaged and rng.random() < .6:
                affordable_repairs = [building for building in run.damaged
                                      if run.cash >= REPAIR_COSTS[building]]
                if affordable_repairs:
                    run.repair(rng.choice(sorted(affordable_repairs)), day)

        if day < 20:
            multiplier = HOUSING_MULTIPLIER[run.levels["housing"] - 1]
            income = rounded(run.population * .1 * multiplier * (.5 if "housing" in run.damaged else 1.0))
            run.cash += income
            capacity = HOUSING_CAPACITY[run.levels["housing"] - 1]
            if "housing" not in run.damaged and run.population < capacity:
                run.population = min(capacity, run.population + 30)
    return run


def randomized_summary(seed_count: int = 500) -> dict:
    runs = [run_random_policy(seed) for seed in range(seed_count)]
    failed_counts = [len(DAYS) - run.defended for run in runs]
    distribution = {str(count): failed_counts.count(count) for count in range(len(DAYS) + 1)}
    return {
        "method": "fixed-seed random affordable purchases and optional repairs; results are descriptive, not a win-rate guarantee",
        "seeds": {"firstInclusive": 0, "lastInclusive": seed_count - 1, "count": seed_count},
        "randomPolicy": {
            "startingCash": 500,
            "dailyPurchaseCountWeightsFor0to3": [5, 55, 35, 5],
            "maximumQuizAnswers": 2,
            "perDayQuizAttemptProbability": .2,
            "riverbedEvent": "removed",
            "perIncidentRepairAttemptProbability": .6,
        },
        "meanUnprotectedIncidents": round(sum(failed_counts) / seed_count, 3),
        "unprotectedIncidentsByCount": distribution,
        "meanFinalCash": round(sum(run.cash for run in runs) / seed_count, 2),
        "meanTotalDeaths": round(sum(run.deaths for run in runs) / seed_count, 2),
        "meanAwareness": round(sum(run.awareness for run in runs) / seed_count, 2),
        "meanQuizAnswers": round(sum(run.quiz_count for run in runs) / seed_count, 2),
        "meanRepairs": round(sum(len(run.repairs) for run in runs) / seed_count, 2),
    }


def build_report() -> dict:
    card_names = ["Kasa", "Beton", "Çadır", "Lider", "Yorgun"]
    runs = []
    for policy in scenarios():
        result = Run(policy.name).run_policy(policy)
        result["cardIndex"] = card_index(result["defendedIncidents"], result["awareness"], result["cash"], result["totalDeaths"])
        result["card"] = card_names[result["cardIndex"]]
        runs.append(result)

    reachable = {index: next((run for run in runs if run["cardIndex"] == index), None)
                 for index in range(len(card_names))}
    baseline_leader = next(run for run in runs if run["cardIndex"] == 3 and "plus housing 3" not in run["policy"])
    extra_leader = next(run for run in runs if "plus housing 3" in run["policy"])
    extra_upgrade_costs = [UPGRADE_COSTS["housing"][1], UPGRADE_COSTS["school"][1]]
    suggestions = []
    for index, run in reachable.items():
        if run is None:
            suggestions.append({"cardIndex": index, "card": card_names[index],
                                "suggestion": "Review only this card's existing threshold; no balance data was changed."})

    return {
        "model": "deterministic 20-day policy simulation; no Unity or third-party packages",
        "startingState": {"cash": 500, "population": 1000, "awareness": 10, "dayLengthSeconds": 12,
                          "speedMultiplier": 2, "eventDays": DAYS},
        "rules": {
            "quizReward": 25, "quizQuestionCount": 12, "schoolCaps": list(SCHOOL_CAP),
            "housingIncomeMultipliers": list(HOUSING_MULTIPLIER), "dailyMigration": 30,
            "dailyIncomePerResident": 0.1, "housingDamageIncomeMultiplier": 0.5,
            "damagePreservesPurchasedLevel": True,
            "riverbedEvent": "removed", "damagedAkutMitigationLevel": 0,
            "casualtyFormula": "round(base * (1 - awareness*0.006) * (1 - activeAkutLevel*0.10))",
        },
        "cardThresholds": {"richCash": 900, "richDeaths": 240, "concreteDefended": 3,
                            "concreteAwarenessBelow": 40, "campDefendedAtMost": 2,
                            "campAwarenessAtLeast": 60, "campDeathsAtMost": 320},
        "runs": runs,
        "extraLevelThreeBudgetCheck": {
            "baselineLeaderEndingCash": baseline_leader["cash"],
            "extraUpgrades": ["housing 2→3 (400)", "school 2→3 (400)"],
            "extraSpend": sum(extra_upgrade_costs),
            "baselineCashLessExtraSpendBeforeIncome": baseline_leader["cash"] - sum(extra_upgrade_costs),
            "extraUpgradedPolicyEndingCash": extra_leader["cash"],
            "extraUpgradedPolicyDefendedIncidents": extra_leader["defendedIncidents"],
            "extraUpgradedPolicyDeaths": extra_leader["totalDeaths"],
            "extraUpgradedPolicyPurchases": extra_leader["purchases"],
            "extraUpgradedPolicyTargetsReached": extra_leader["targetsReached"],
        },
        "randomSpendingSummary": randomized_summary(),
        "cardsReachable": all(run is not None for run in reachable.values()),
        "reachableCards": {card_names[index]: (run["policy"] if run else None)
                            for index, run in reachable.items()},
        "thresholdSuggestions": suggestions,
    }


if __name__ == "__main__":
    report = build_report()
    OUTPUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(OUTPUT.relative_to(ROOT)),
                      "cardsReachable": report["cardsReachable"],
                      "reachableCards": report["reachableCards"],
                      "runs": [{"card": run["card"], "cash": run["cash"], "awareness": run["awareness"],
                                "defended": run["defendedIncidents"], "deaths": run["totalDeaths"],
                                "repairs": run["repairs"], "failedTargets": run["failedTargets"]}
                               for run in report["runs"]],
                      "extraLevelThreeBudgetCheck": report["extraLevelThreeBudgetCheck"],
                      "randomSpendingSummary": report["randomSpendingSummary"],
                      "thresholdSuggestions": report["thresholdSuggestions"]},
                     ensure_ascii=False, indent=2))
