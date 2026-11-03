import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/Button'
import { Field } from '@/components/ui/Field'
import { Input } from '@/components/ui/FormControls'
import { FormError } from '@/components/ui/FormError'
import { describeError } from '@/lib/api/errors'
import { useAuth } from '@/lib/auth/AuthContext'
import { AuthLayout } from './AuthLayout'
import { DEMO_ACCOUNT } from './demo'
import { type LoginValues, loginSchema } from './schemas'

export default function LoginPage() {
  const { login } = useAuth()
  const [params] = useSearchParams()
  const [error, setError] = useState<string | null>(null)
  const [demoLoading, setDemoLoading] = useState(false)
  const form = useForm<LoginValues>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })
  const { errors, isSubmitting } = form.formState

  const signIn = async ({ email, password }: LoginValues) => {
    setError(null)
    try {
      await login(email, password)
    } catch (e) {
      setError(describeError(e))
    }
  }

  const signInAsDemo = async () => {
    setDemoLoading(true)
    await signIn(DEMO_ACCOUNT)
    setDemoLoading(false)
  }

  const registerLink = params.get('next') ? `/register?next=${encodeURIComponent(params.get('next') ?? '')}` : '/register'

  return (
    <AuthLayout
      title="Sign in to bid"
      description="Welcome back. Your bids and watchlist are where you left them."
      footer={
        <>
          New to Gavel?{' '}
          <Link to={registerLink} className="font-medium text-cobalt-600 hover:underline">
            Create an account
          </Link>
        </>
      }
    >
      <div className="grid gap-5">
        <Button variant="secondary" size="lg" loading={demoLoading} onClick={() => void signInAsDemo()}>
          Try the demo account
        </Button>
        <div className="flex items-center gap-3 text-sm text-muted">
          <span className="h-px flex-1 bg-line" />
          or use your email
          <span className="h-px flex-1 bg-line" />
        </div>
        <form className="grid gap-4" onSubmit={form.handleSubmit(signIn)} noValidate>
          <FormError message={error} />
          <Field label="Email" error={errors.email?.message}>
            {(props) => <Input type="email" autoComplete="email" {...props} {...form.register('email')} />}
          </Field>
          <Field label="Password" error={errors.password?.message}>
            {(props) => <Input type="password" autoComplete="current-password" {...props} {...form.register('password')} />}
          </Field>
          <Button type="submit" size="lg" loading={isSubmitting && !demoLoading}>
            Sign in
          </Button>
        </form>
      </div>
    </AuthLayout>
  )
}
