class Handler:
    def __init__(self, next=None):
        self.next = next

    def handle(self, amount):
        if self.next:
            return self.next.handle(amount)
        return "Ніхто не обробив платіж"


class SmallPayment(Handler):
    def handle(self, amount):
        if amount <= 100:
            return f"Оплачено через SmallPayment: {amount}"
        return super().handle(amount)


class MediumPayment(Handler):
    def handle(self, amount):
        if amount <= 1000:
            return f"Оплачено через MediumPayment: {amount}"
        return super().handle(amount)


class LargePayment(Handler):
    def handle(self, amount):
        if amount > 1000:
            return f"Оплачено через LargePayment: {amount}"
        return super().handle(amount)

chain = SmallPayment(MediumPayment(LargePayment()))
print(chain.handle(50))
print(chain.handle(500))
print(chain.handle(5000))