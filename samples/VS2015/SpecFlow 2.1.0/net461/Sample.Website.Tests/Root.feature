Feature: Issue 27

Scenario: Step definition skeletons are provided
	Given I don't have step definitions
	When I run the test
	Then the step definition skeletons are provided in the test output